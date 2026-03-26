using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
}