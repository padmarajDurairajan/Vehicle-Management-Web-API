using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    private readonly ILogger<VehicleRepository> _logger;

    public VehicleRepository(AppDbContext context, ILogger<VehicleRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    public override async Task<List<Vehicle>> GetAllAsync()
        => await _context.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.Id)
            .ToListAsync();

    public override async Task<Vehicle?> GetByIdAsync(int id)
        => await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<bool> RegistrationExistsAsync(string registrationNumber, int? excludeId = null)
        => await _context.Vehicles.AnyAsync(v =>
            v.RegistrationNumber == registrationNumber &&
            (excludeId == null || v.Id != excludeId.Value));

    public override async Task<Vehicle> AddAsync(Vehicle vehicle)
    {
        try
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error while adding vehicle. RegNo={RegNo}", vehicle.RegistrationNumber);
            throw;
        }
        finally
        {
            _logger.LogDebug("AddAsync(Vehicle) finished.");
        }
    }

    public override async Task<bool> UpdateAsync(Vehicle vehicle)
    {
        try
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error while updating vehicle. Id={Id} RegNo={RegNo}",
                vehicle.Id, vehicle.RegistrationNumber);
            throw;
        }
        finally
        {
            _logger.LogDebug("UpdateAsync(Vehicle) finished. Id={Id}", vehicle.Id);
        }
    }

    public override async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
            if (existing is null) return false;

            existing.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error while soft deleting vehicle. Id={Id}", id);
            throw;
        }
        finally
        {
            _logger.LogDebug("DeleteAsync(Vehicle) finished. Id={Id}", id);
        }
    }

    public async Task<List<Vehicle>> GetByCustomerIdAsync(int customerId)
        => await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderBy(v => v.Id)
            .ToListAsync();
}