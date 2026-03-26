using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public override async Task<List<Customer>> GetAllAsync()
        => await _context.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .ToListAsync();

    public override async Task<Customer?> GetByIdAsync(int id)
        => await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        => await _context.Customers.AnyAsync(c =>
            c.Email == email && (excludeId == null || c.Id != excludeId.Value));
}