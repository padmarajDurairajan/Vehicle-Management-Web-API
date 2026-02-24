using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(int id);
    Task<bool> RegistrationExistsAsync(string registrationNumber, int? excludeId = null);
    Task<Vehicle> AddAsync(Vehicle vehicle);
    Task<bool> UpdateAsync(Vehicle vehicle);
    Task<bool> DeleteAsync(int id);
}