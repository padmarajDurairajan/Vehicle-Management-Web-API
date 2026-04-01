namespace VehicleManagementApi.Pulsar;

public sealed class PulsarPublishQueueItem
{
    public string Topic { get; init; } = "";
    public string EventName { get; init; } = "";
    public byte[] Payload { get; init; } = Array.Empty<byte>();
}