using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;
    public CustomerRepository(AppDbContext context) => _context = context;

    public async Task<List<Customer>> GetAllAsync()
        => await _context.Customers.AsNoTracking().ToListAsync();

    public async Task<Customer?> GetByIdAsync(int id)
        => await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        => await _context.Customers.AnyAsync(c =>
            c.Email == email && (excludeId == null || c.Id != excludeId.Value));

    public async Task<Customer> AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null) return false;

        _context.Customers.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}