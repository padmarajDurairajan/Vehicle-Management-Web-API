using System.ComponentModel.DataAnnotations;

namespace VehicleManagementApi.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}