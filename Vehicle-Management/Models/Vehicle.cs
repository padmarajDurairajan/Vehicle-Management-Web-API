using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleManagementApi.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Make { get; set; } = "";

    [Required, MaxLength(50)]
    public string Model { get; set; } = "";

    [Range(1886, 2100)]
    public int Year { get; set; }

    [Required, MaxLength(20)]
    public string RegistrationNumber { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }
}