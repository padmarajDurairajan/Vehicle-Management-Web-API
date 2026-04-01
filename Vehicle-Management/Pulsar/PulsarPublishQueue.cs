using System.Threading.Channels;

namespace VehicleManagementApi.Pulsar;

public sealed class PulsarPublishQueue : IPulsarPublishQueue
{
    private readonly Channel<PulsarPublishQueueItem> _channel;

    public PulsarPublishQueue()
    {
        _channel = Channel.CreateUnbounded<PulsarPublishQueueItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(PulsarPublishQueueItem item, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(item, cancellationToken);

    public ValueTask<PulsarPublishQueueItem> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}