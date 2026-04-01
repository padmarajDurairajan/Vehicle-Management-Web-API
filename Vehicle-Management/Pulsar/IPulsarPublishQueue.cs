namespace VehicleManagementApi.Pulsar;

public interface IPulsarPublishQueue
{
    ValueTask EnqueueAsync(PulsarPublishQueueItem item, CancellationToken cancellationToken = default);
    ValueTask<PulsarPublishQueueItem> DequeueAsync(CancellationToken cancellationToken);
}