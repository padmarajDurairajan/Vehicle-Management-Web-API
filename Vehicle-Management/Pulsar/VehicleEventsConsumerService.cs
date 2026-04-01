using System.Text;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VehicleManagementApi.Pulsar;

public sealed class VehicleEventsConsumerService : BackgroundService
{
    private readonly PulsarOptions _options;
    private readonly ILogger<VehicleEventsConsumerService> _logger;
    private readonly IPulsarClient _client;

    public VehicleEventsConsumerService(
        IOptions<PulsarOptions> options,
        ILogger<VehicleEventsConsumerService> logger)
    {
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
            _logger.LogInformation("Vehicle Pulsar consumer is disabled.");
            return;
        }

        var consumer = _client
            .NewConsumer(Schema.ByteArray)
            .Topic(_options.VehicleEventsTopic)
            .SubscriptionName(_options.VehicleSubscriptionName)
            .InitialPosition(SubscriptionInitialPosition.Latest)
            .Create();

        _logger.LogInformation(
            "Vehicle Pulsar consumer started. Topic={Topic} Subscription={Subscription}",
            _options.VehicleEventsTopic,
            _options.VehicleSubscriptionName);

        try
        {
            await foreach (var message in consumer.Messages(stoppingToken))
            {
                try
                {
                    var data = message.Value();
                    var json = Encoding.UTF8.GetString(data);

                    _logger.LogInformation(
                        "Vehicle event consumed. Topic={Topic} Message={Message}",
                        _options.VehicleEventsTopic,
                        json);

                    await consumer.Acknowledge(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing vehicle event.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Vehicle Pulsar consumer stopping.");
        }
        finally
        {
            await consumer.DisposeAsync();
            await _client.DisposeAsync();
        }
    }
}