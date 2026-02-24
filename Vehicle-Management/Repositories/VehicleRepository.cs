using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;
    public VehicleRepository(AppDbContext context) => _context = context;

    public async Task<List<Vehicle>> GetAllAsync()
        => await _context.Vehicles.AsNoTracking().ToListAsync();

    public async Task<Vehicle?> GetByIdAsync(int id)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<bool> RegistrationExistsAsync(string registrationNumber, int? excludeId = null)
        => await _context.Vehicles.AnyAsync(v =>
            v.RegistrationNumber == registrationNumber && (excludeId == null || v.Id != excludeId.Value));

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        _context.Vehicles.Add(vehicle);
        await _context.SaveChangesAsync();
        return vehicle;
    }

    public async Task<bool> UpdateAsync(Vehicle vehicle)
    {
        _context.Vehicles.Update(vehicle);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (existing is null) return false;

        _context.Vehicles.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}