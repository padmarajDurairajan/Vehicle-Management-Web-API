using VehicleManagementApi.Models;

namespace VehicleManagementApi.Services;

public interface IVehicleService
{
    Task<List<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, Vehicle? Vehicle)> CreateAsync(Vehicle input);
    Task<(bool Success, string? Error)> UpdateAsync(int id, Vehicle input);
    Task<bool> DeleteAsync(int id);
}