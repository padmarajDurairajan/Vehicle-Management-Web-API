namespace VehicleManagementApi.Pulsar;

public sealed class PulsarOptions
{
    public const string SectionName = "Pulsar";

    public bool Enabled { get; set; } = false;
    public string ServiceUrl { get; set; } = "pulsar://localhost:6650";
    public string CustomerEventsTopic { get; set; } = "persistent://public/default/customer-events";
    public string VehicleEventsTopic { get; set; } = "persistent://public/default/vehicle-events";
}