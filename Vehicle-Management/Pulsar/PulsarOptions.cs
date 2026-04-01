namespace VehicleManagementApi.Pulsar;

public sealed class PulsarOptions
{
    public const string SectionName = "Pulsar";

    public bool Enabled { get; set; } = false;
    public string ServiceUrl { get; set; } = "pulsar://localhost:6650";

    public string CustomerEventsTopic { get; set; } = "persistent://public/default/customer-events";
    public string VehicleEventsTopic { get; set; } = "persistent://public/default/vehicle-events";

    public string CustomerSubscriptionName { get; set; } = "vehicle-management-customer-sub";
    public string VehicleSubscriptionName { get; set; } = "vehicle-management-vehicle-sub";

    public int PublishTimeoutSeconds { get; set; } = 10;
}