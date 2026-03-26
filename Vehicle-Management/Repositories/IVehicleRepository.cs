using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<bool> RegistrationExistsAsync(string registrationNumber, int? excludeId = null);
    Task<List<Vehicle>> GetByCustomerIdAsync(int customerId);
}