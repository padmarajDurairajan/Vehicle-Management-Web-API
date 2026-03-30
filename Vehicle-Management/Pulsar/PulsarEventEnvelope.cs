namespace VehicleManagementApi.Pulsar;

public sealed class PulsarEventEnvelope<T>
{
    public string EventName { get; init; } = "";
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public T Payload { get; init; } = default!;
}