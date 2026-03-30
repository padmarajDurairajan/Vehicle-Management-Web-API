namespace VehicleManagementApi.Pulsar;

public sealed class CustomerEventData
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}