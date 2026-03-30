using Microsoft.Extensions.Caching.Memory;
using VehicleManagementApi.Caching;
using VehicleManagementApi.Models;
using VehicleManagementApi.Pulsar;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class VehicleService : IVehicleService
{
    private static readonly TimeSpan AllVehiclesCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VehicleByIdCacheDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan VehiclesByCustomerCacheDuration = TimeSpan.FromMinutes(3);

    private readonly IVehicleRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VehicleService> _logger;
    private readonly IPulsarEventPublisher _pulsarPublisher;

    public VehicleService(
        IVehicleRepository repo,
        IMemoryCache cache,
        ILogger<VehicleService> logger,
        IPulsarEventPublisher pulsarPublisher)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
        _pulsarPublisher = pulsarPublisher;
    }

    public async Task<List<Vehicle>> GetAllAsync()
    {
        if (_cache.TryGetValue(CacheKeys.Vehicles.All, out List<Vehicle>? cachedVehicles) &&
            cachedVehicles is not null)
        {
            _logger.LogInformation("CACHE HIT - Vehicles");
            return cachedVehicles;
        }

        _logger.LogInformation("CACHE MISS - Vehicles (DB call)");

        var vehicles = await _repo.GetAllAsync();

        _cache.Set(
            CacheKeys.Vehicles.All,
            vehicles,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AllVehiclesCacheDuration
            });

        return vehicles;
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        var cacheKey = CacheKeys.Vehicles.ById(id);

        if (_cache.TryGetValue(cacheKey, out Vehicle? cachedVehicle))
        {
            _logger.LogInformation("CACHE HIT - Vehicle Id={Id}", id);
            return cachedVehicle;
        }

        _logger.LogInformation("CACHE MISS - Vehicle Id={Id} (DB call)", id);

        var vehicle = await _repo.GetByIdAsync(id);

        if (vehicle is not null)
        {
            _cache.Set(
                cacheKey,
                vehicle,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = VehicleByIdCacheDuration
                });
        }

        return vehicle;
    }

    public async Task<(bool Success, string? Error, Vehicle? Vehicle)> CreateAsync(Vehicle input)
    {
        _logger.LogInformation("Creating vehicle. RegNo={RegNo}", input.RegistrationNumber);

        var exists = await _repo.RegistrationExistsAsync(input.RegistrationNumber);
        if (exists)
        {
            _logger.LogWarning("Create vehicle failed: duplicate RegNo={RegNo}", input.RegistrationNumber);
            return (false, "RegistrationNumber already exists.", null);
        }

        input.Id = 0;

        var created = await _repo.AddAsync(input);

        InvalidateVehicleCaches(created.Id, created.CustomerId);
        await _pulsarPublisher.PublishVehicleAsync("vehicle.created", created);

        _logger.LogInformation("Vehicle created. Id={Id} RegNo={RegNo}", created.Id, created.RegistrationNumber);
        return (true, null, created);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Vehicle input)
    {
        _logger.LogInformation("Updating vehicle. Id={Id}", id);

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Update vehicle failed: not found. Id={Id}", id);
            return (false, "Not found.");
        }

        var previousCustomerId = existing.CustomerId;

        var regConflict = await _repo.RegistrationExistsAsync(input.RegistrationNumber, excludeId: id);
        if (regConflict)
        {
            _logger.LogWarning(
                "Update vehicle failed: duplicate RegNo={RegNo} Id={Id}",
                input.RegistrationNumber,
                id);

            return (false, "RegistrationNumber already exists.");
        }

        existing.Make = input.Make;
        existing.Model = input.Model;
        existing.Year = input.Year;
        existing.RegistrationNumber = input.RegistrationNumber;
        existing.IsActive = input.IsActive;
        existing.CustomerId = input.CustomerId;

        await _repo.UpdateAsync(existing);

        InvalidateVehicleCaches(id, previousCustomerId);

        if (input.CustomerId.HasValue && input.CustomerId != previousCustomerId)
            _cache.Remove(CacheKeys.Vehicles.ByCustomerId(input.CustomerId.Value));

        await _pulsarPublisher.PublishVehicleAsync("vehicle.updated", existing);

        _logger.LogInformation("Vehicle updated successfully. Id={Id}", id);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting vehicle. Id={Id}", id);

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Delete vehicle failed: not found. Id={Id}", id);
            return false;
        }

        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
        {
            _logger.LogWarning("Delete vehicle failed during repository delete. Id={Id}", id);
            return false;
        }

        existing.IsActive = false;

        InvalidateVehicleCaches(id, existing.CustomerId);
        await _pulsarPublisher.PublishVehicleAsync("vehicle.deleted", existing);

        _logger.LogInformation("Vehicle deleted successfully. Id={Id}", id);
        return true;
    }

    public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
    {
        var cacheKey = CacheKeys.Vehicles.ByCustomerId(customerId);

        if (_cache.TryGetValue(cacheKey, out List<Vehicle>? cachedVehicles) &&
            cachedVehicles is not null)
        {
            _logger.LogInformation("CACHE HIT - Vehicles for CustomerId={CustomerId}", customerId);
            return cachedVehicles;
        }

        _logger.LogInformation("CACHE MISS - Vehicles for CustomerId={CustomerId} (DB call)", customerId);

        var vehicles = await _repo.GetByCustomerIdAsync(customerId);

        _cache.Set(
            cacheKey,
            vehicles,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = VehiclesByCustomerCacheDuration
            });

        return vehicles;
    }

    private void InvalidateVehicleCaches(int vehicleId, int? customerId)
    {
        _logger.LogInformation(
            "Invalidating vehicle cache. VehicleId={VehicleId}, CustomerId={CustomerId}",
            vehicleId,
            customerId);

        _cache.Remove(CacheKeys.Vehicles.All);
        _cache.Remove(CacheKeys.Vehicles.ById(vehicleId));

        if (customerId.HasValue)
            _cache.Remove(CacheKeys.Vehicles.ByCustomerId(customerId.Value));
    }
}