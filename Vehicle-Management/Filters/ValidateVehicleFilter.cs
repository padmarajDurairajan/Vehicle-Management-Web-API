using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Filters;

public class ValidateVehicleFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("input", out var value) || value is not Vehicle input)
            return;

        if (input.Year < 1886 || input.Year > 2100)
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "Year must be between 1886 and 2100."
            });
            return;
        }

        var pattern = @"^[A-Z]{2}-\d{2}-[A-Z]{2}-\d{4}$";
        if (string.IsNullOrWhiteSpace(input.RegistrationNumber) ||
            !Regex.IsMatch(input.RegistrationNumber.Trim().ToUpperInvariant(), pattern))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "RegistrationNumber format must be like TN-01-AB-1234."
            });
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Nothing needed after action executes
    }
}