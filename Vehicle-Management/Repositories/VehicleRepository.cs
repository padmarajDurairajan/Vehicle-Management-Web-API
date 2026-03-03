using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<VehicleRepository> _logger;
    public VehicleRepository(AppDbContext context, ILogger<VehicleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Vehicle>> GetAllAsync()
        => await _context.Vehicles.AsNoTracking().OrderBy(v => v.Id).ToListAsync();

    public async Task<Vehicle?> GetByIdAsync(int id)
        => await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<bool> RegistrationExistsAsync(string registrationNumber, int? excludeId = null)
        => await _context.Vehicles.AnyAsync(v =>
            v.RegistrationNumber == registrationNumber && (excludeId == null || v.Id != excludeId.Value));

    public async Task<Vehicle> AddAsync(Vehicle vehicle)
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

    public async Task<bool> UpdateAsync(Vehicle vehicle)
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

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
            if (existing is null) return false;

            _context.Vehicles.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB error while deleting vehicle. Id={Id}", id);
            throw;
        }
        finally
        {
            _logger.LogDebug("DeleteAsync(Vehicle) finished. Id={Id}", id);
        }
    }
}