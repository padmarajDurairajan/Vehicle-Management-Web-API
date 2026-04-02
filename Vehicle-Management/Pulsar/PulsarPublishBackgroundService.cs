using System.Collections.Concurrent;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Options;

namespace VehicleManagementApi.Pulsar;

public sealed class PulsarPublishBackgroundService : BackgroundService
{
    private readonly IPulsarPublishQueue _queue;
    private readonly PulsarOptions _options;
    private readonly ILogger<PulsarPublishBackgroundService> _logger;
    private readonly IPulsarClient _client;
    private readonly ConcurrentDictionary<string, IProducer<byte[]>> _producers = new();
    private readonly SemaphoreSlim _producerLock = new(1, 1);

    public PulsarPublishBackgroundService(
        IPulsarPublishQueue queue,
        IOptions<PulsarOptions> options,
        ILogger<PulsarPublishBackgroundService> logger)
    {
        _queue = queue;
        _options = options.Value;
        _logger = logger;

        _client = PulsarClient
            .Builder()
            .ServiceUrl(new Uri(_options.ServiceUrl))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Pulsar publish background service is disabled.");
            return;
        }

        _logger.LogInformation("Pulsar publish background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await _queue.DequeueAsync(stoppingToken);
                var producer = await GetProducerAsync(item.Topic, stoppingToken);

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.PublishTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

                // DotPulsar producer.Send handles connection readiness internally.
                // Waiting on state transitions can hang if the producer is already connected.
                _logger.LogInformation(
                    "Background publish waiting to send. Topic={Topic} EventName={EventName}",
                    item.Topic,
                    item.EventName);

                _logger.LogInformation(
                    "Background publish started. Topic={Topic} EventName={EventName}",
                    item.Topic,
                    item.EventName);

                await producer.Send(item.Payload, linkedCts.Token);

                _logger.LogInformation(
                    "Background publish completed. Topic={Topic} EventName={EventName}",
                    item.Topic,
                    item.EventName);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Background Pulsar publish timed out.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background Pulsar publish failed.");
            }
        }
    }

    private async Task<IProducer<byte[]>> GetProducerAsync(string topic, CancellationToken cancellationToken)
    {
        if (_producers.TryGetValue(topic, out var existingProducer))
            return existingProducer;

        await _producerLock.WaitAsync(cancellationToken);
        try
        {
            if (_producers.TryGetValue(topic, out existingProducer))
                return existingProducer;

            var producer = _client
                .NewProducer(Schema.ByteArray)
                .Topic(topic)
                .Create();

            _producers[topic] = producer;

            _logger.LogInformation("Created Pulsar producer for topic {Topic}", topic);

            return producer;
        }
        finally
        {
            _producerLock.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        foreach (var producer in _producers.Values)
            await producer.DisposeAsync();

        await _client.DisposeAsync();
        _producerLock.Dispose();
    }
}
