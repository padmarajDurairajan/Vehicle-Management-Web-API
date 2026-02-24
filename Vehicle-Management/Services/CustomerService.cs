using VehicleManagementApi.Models;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    public CustomerService(ICustomerRepository repo) => _repo = repo;

    public Task<List<Customer>> GetAllAsync() => _repo.GetAllAsync();

    public Task<Customer?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<(bool Success, string? Error, Customer? Customer)> CreateAsync(Customer input)
    {
        var emailExists = await _repo.EmailExistsAsync(input.Email);
        if (emailExists) return (false, "Email already exists.", null);

        input.Id = 0;
        input.CreatedAtUtc = DateTime.UtcNow;

        var created = await _repo.AddAsync(input);
        return (true, null, created);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Customer input)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null) return (false, "Not found.");

        var emailConflict = await _repo.EmailExistsAsync(input.Email, excludeId: id);
        if (emailConflict) return (false, "Email already exists.");

        existing.FullName = input.FullName;
        existing.Email = input.Email;
        existing.Phone = input.Phone;
        existing.Address = input.Address;

        await _repo.UpdateAsync(existing);
        return (true, null);
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}