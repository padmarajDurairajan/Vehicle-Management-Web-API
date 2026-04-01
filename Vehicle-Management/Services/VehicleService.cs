using VehicleManagementApi.Caching;
using VehicleManagementApi.Models;
using VehicleManagementApi.Pulsar;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class VehicleService : IVehicleService
{
    private static readonly TimeSpan AllVehiclesMemoryCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AllVehiclesDistributedCacheDuration = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan VehicleByIdMemoryCacheDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan VehicleByIdDistributedCacheDuration = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan VehiclesByCustomerMemoryCacheDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan VehiclesByCustomerDistributedCacheDuration = TimeSpan.FromMinutes(10);

    private readonly IVehicleRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<VehicleService> _logger;
    private readonly IPulsarEventPublisher _pulsarPublisher;

    public VehicleService(
        IVehicleRepository repo,
        ICacheService cache,
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
        return await _cache.GetOrCreateAsync(
            CacheKeys.Vehicles.All,
            async _ =>
            {
                _logger.LogInformation("CACHE MISS - Vehicles (DB call)");
                return await _repo.GetAllAsync();
            },
            AllVehiclesMemoryCacheDuration,
            AllVehiclesDistributedCacheDuration) ?? new List<Vehicle>();
    }

    public async Task<Vehicle?> GetByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.Vehicles.ById(id),
            async _ =>
            {
                _logger.LogInformation("CACHE MISS - Vehicle Id={Id} (DB call)", id);
                return await _repo.GetByIdAsync(id);
            },
            VehicleByIdMemoryCacheDuration,
            VehicleByIdDistributedCacheDuration);
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

        await InvalidateVehicleCachesAsync(created.Id, created.CustomerId);

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

        await InvalidateVehicleCachesAsync(id, previousCustomerId);

        if (input.CustomerId.HasValue && input.CustomerId != previousCustomerId)
        {
            await _cache.RemoveAsync(CacheKeys.Vehicles.ByCustomerId(input.CustomerId.Value));
        }

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

        await InvalidateVehicleCachesAsync(id, existing.CustomerId);

        await _pulsarPublisher.PublishVehicleAsync("vehicle.deleted", existing);

        _logger.LogInformation("Vehicle deleted successfully. Id={Id}", id);
        return true;
    }

    public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.Vehicles.ByCustomerId(customerId),
            async _ =>
            {
                _logger.LogInformation("CACHE MISS - Vehicles for CustomerId={CustomerId} (DB call)", customerId);
                return await _repo.GetByCustomerIdAsync(customerId);
            },
            VehiclesByCustomerMemoryCacheDuration,
            VehiclesByCustomerDistributedCacheDuration) ?? new List<Vehicle>();
    }

    private async Task InvalidateVehicleCachesAsync(int vehicleId, int? customerId)
    {
        _logger.LogInformation(
            "Invalidating vehicle cache. VehicleId={VehicleId}, CustomerId={CustomerId}",
            vehicleId,
            customerId);

        await _cache.RemoveAsync(CacheKeys.Vehicles.All);
        await _cache.RemoveAsync(CacheKeys.Vehicles.ById(vehicleId));

        if (customerId.HasValue)
        {
            await _cache.RemoveAsync(CacheKeys.Vehicles.ByCustomerId(customerId.Value));
        }
    }
}