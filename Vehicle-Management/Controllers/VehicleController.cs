using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Database;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly AppDbContext _context;
    public VehiclesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<List<Vehicle>>> GetAll()
        => Ok(await _context.Vehicles.AsNoTracking().ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Vehicle>> GetById(int id)
    {
        var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpPost]
    public async Task<ActionResult<Vehicle>> Create(Vehicle input)
    {
        var exists = await _context.Vehicles.AnyAsync(v => v.RegistrationNumber == input.RegistrationNumber);
        if (exists) return Conflict("RegistrationNumber already exists.");

        _context.Vehicles.Add(input);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Vehicle input)
    {
        var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (existing is null) return NotFound();

        var regConflict = await _context.Vehicles.AnyAsync(v => v.Id != id && v.RegistrationNumber == input.RegistrationNumber);
        if (regConflict) return Conflict("RegistrationNumber already exists.");

        existing.Make = input.Make;
        existing.Model = input.Model;
        existing.Year = input.Year;
        existing.RegistrationNumber = input.RegistrationNumber;
        existing.IsActive = input.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
        if (existing is null) return NotFound();

        _context.Vehicles.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
