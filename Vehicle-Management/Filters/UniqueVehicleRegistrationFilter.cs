using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VehicleManagementApi.Models;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Filters;

public class UniqueVehicleRegistrationFilter : IAsyncActionFilter
{
    private readonly IVehicleRepository _repo;

    public UniqueVehicleRegistrationFilter(IVehicleRepository repo)
    {
        _repo = repo;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("input", out var value) || value is not Vehicle input)
        {
            await next();
            return;
        }

        int? excludeId = null;
        if (context.RouteData.Values.TryGetValue("id", out var idObj) &&
            int.TryParse(idObj?.ToString(), out var id))
        {
            excludeId = id;
        }

        var exists = await _repo.RegistrationExistsAsync(input.RegistrationNumber, excludeId);
        if (exists)
        {
            context.Result = new ConflictObjectResult(new
            {
                error = "RegistrationNumber already exists."
            });
            return;
        }

        await next();
    }
}