using VehicleManagementApi.Models;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repo;
    public VehicleService(IVehicleRepository repo) => _repo = repo;

    public Task<List<Vehicle>> GetAllAsync() => _repo.GetAllAsync();

    public Task<Vehicle?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<(bool Success, string? Error, Vehicle? Vehicle)> CreateAsync(Vehicle input)
    {
        var exists = await _repo.RegistrationExistsAsync(input.RegistrationNumber);
        if (exists) return (false, "RegistrationNumber already exists.", null);

        input.Id = 0;
        var created = await _repo.AddAsync(input);
        return (true, null, created);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Vehicle input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return (false, "Not found.");

        var regConflict = await _repo.RegistrationExistsAsync(input.RegistrationNumber, excludeId: id);
        if (regConflict) return (false, "RegistrationNumber already exists.");

        existing.Make = input.Make;
        existing.Model = input.Model;
        existing.Year = input.Year;
        existing.RegistrationNumber = input.RegistrationNumber;
        existing.IsActive = input.IsActive;

        await _repo.UpdateAsync(existing);
        return (true, null);
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}