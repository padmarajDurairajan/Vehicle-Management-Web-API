using Microsoft.AspNetCore.Mvc;
using VehicleManagementApi.Models;
using VehicleManagementApi.Services;

namespace VehicleManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(Customer input)
    {
        var (success, error, created) = await _service.CreateAsync(input);
        if (!success) return Conflict(error);

        return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Customer input)
    {
        var (success, error) = await _service.UpdateAsync(id, input);
        if (!success) return error == "Not found." ? NotFound() : Conflict(error);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}