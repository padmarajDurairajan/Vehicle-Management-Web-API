using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleManagementApi.Filters;
using VehicleManagementApi.Models;
using VehicleManagementApi.Services;

namespace VehicleManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require JWT (also enforced by FallbackPolicy)
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _service;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(IVehicleService service, ILogger<VehiclesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Vehicle>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Vehicle>> GetById(int id)
    {
        var vehicle = await _service.GetByIdAsync(id);
        return vehicle is null ? NotFound() : Ok(vehicle);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ServiceFilter(typeof(ValidateVehicleFilter))]
    [ServiceFilter(typeof(UniqueVehicleRegistrationFilter))]
    public async Task<ActionResult<Vehicle>> Create(Vehicle input)
    {
        _logger.LogInformation("POST /api/vehicles called. RegNo={RegNo}", input.RegistrationNumber);

        var (success, error, created) = await _service.CreateAsync(input);
        if (!success) return Conflict(error);

        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ServiceFilter(typeof(ValidateVehicleFilter))]
    [ServiceFilter(typeof(UniqueVehicleRegistrationFilter))]
    public async Task<IActionResult> Update(int id, Vehicle input)
    {
        var (success, error) = await _service.UpdateAsync(id, input);
        if (!success) return error == "Not found." ? NotFound() : Conflict(error);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}