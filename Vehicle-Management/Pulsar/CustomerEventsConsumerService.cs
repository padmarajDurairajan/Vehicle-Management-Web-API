using System.Text;
using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace VehicleManagementApi.Pulsar;

public sealed class CustomerEventsConsumerService : BackgroundService
{
    private readonly PulsarOptions _options;
    private readonly ILogger<CustomerEventsConsumerService> _logger;
    private readonly IPulsarClient _client;

    public CustomerEventsConsumerService(
        IOptions<PulsarOptions> options,
        ILogger<CustomerEventsConsumerService> logger)
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
            _logger.LogInformation("Customer Pulsar consumer is disabled.");
            return;
        }

        var consumer = _client
            .NewConsumer(Schema.ByteArray)
            .Topic(_options.CustomerEventsTopic)
            .SubscriptionName(_options.CustomerSubscriptionName)
            .InitialPosition(SubscriptionInitialPosition.Latest)
            .Create();

        _logger.LogInformation(
            "Customer Pulsar consumer started. Topic={Topic} Subscription={Subscription}",
            _options.CustomerEventsTopic,
            _options.CustomerSubscriptionName);

        try
        {
            await foreach (var message in consumer.Messages(stoppingToken))
            {
                try
                {
                    var data = message.Value();
                    var json = Encoding.UTF8.GetString(data);

                    _logger.LogInformation(
                        "Customer event consumed. Topic={Topic} Message={Message}",
                        _options.CustomerEventsTopic,
                        json);

                    await consumer.Acknowledge(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing customer event.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Customer Pulsar consumer stopping.");
        }
        finally
        {
            await consumer.DisposeAsync();
            await _client.DisposeAsync();
        }
    }
}