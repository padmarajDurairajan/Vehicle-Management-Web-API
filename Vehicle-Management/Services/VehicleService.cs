using VehicleManagementApi.Models;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ILogger<VehicleService> _logger;
    public VehicleService(IVehicleRepository repo, ILogger<VehicleService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<List<Vehicle>> GetAllAsync() => _repo.GetAllAsync();

    public Task<Vehicle?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

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

        var regConflict = await _repo.RegistrationExistsAsync(input.RegistrationNumber, excludeId: id);
        if (regConflict)
        {
            _logger.LogWarning("Update vehicle failed: duplicate RegNo={RegNo} Id={Id}",
                input.RegistrationNumber, id);
            return (false, "RegistrationNumber already exists.");
        }

        existing.Make = input.Make;
        existing.Model = input.Model;
        existing.Year = input.Year;
        existing.RegistrationNumber = input.RegistrationNumber;
        existing.IsActive = input.IsActive;

        await _repo.UpdateAsync(existing);

        _logger.LogInformation("Vehicle updated successfully. Id={Id}", id);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting vehicle. Id={Id}", id);

        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
        {
            _logger.LogWarning("Delete vehicle failed: not found. Id={Id}", id);
            return false;
        }

        _logger.LogInformation("Vehicle deleted successfully. Id={Id}", id);
        return true;
    }

    public Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
    => _repo.GetByCustomerIdAsync(customerId);
}