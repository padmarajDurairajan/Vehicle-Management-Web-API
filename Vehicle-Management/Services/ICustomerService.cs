using VehicleManagementApi.Models;

namespace VehicleManagementApi.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<(bool Success, string? Error, Customer? Customer)> CreateAsync(Customer input);
    Task<(bool Success, string? Error)> UpdateAsync(int id, Customer input);
    Task<bool> DeleteAsync(int id);
}