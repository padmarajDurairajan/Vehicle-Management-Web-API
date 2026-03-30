namespace VehicleManagementApi.Pulsar;

public sealed class VehicleEventData
{
    public int Id { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Year { get; init; }
    public string RegistrationNumber { get; init; } = "";
    public bool IsActive { get; init; }
    public int? CustomerId { get; init; }
}