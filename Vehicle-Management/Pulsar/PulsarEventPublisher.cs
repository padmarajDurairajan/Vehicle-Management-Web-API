using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Pulsar;

public sealed class PulsarEventPublisher : IPulsarEventPublisher, IAsyncDisposable
{
    private readonly PulsarOptions _options;
    private readonly ILogger<PulsarEventPublisher> _logger;
    private readonly IPulsarClient _client;
    private readonly ConcurrentDictionary<string, IProducer<byte[]>> _producers = new();
    private readonly SemaphoreSlim _producerLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PulsarEventPublisher(
        IOptions<PulsarOptions> options,
        ILogger<PulsarEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        _client = PulsarClient
            .Builder()
            .ServiceUrl(new Uri(_options.ServiceUrl))
            .Build();
    }

    public async Task PublishCustomerAsync(string eventName, Customer customer, CancellationToken cancellationToken = default)
    {
        var payload = new CustomerEventData
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            CreatedAtUtc = customer.CreatedAtUtc
        };

        var envelope = new PulsarEventEnvelope<CustomerEventData>
        {
            EventName = eventName,
            OccurredAtUtc = DateTime.UtcNow,
            Payload = payload
        };

        await PublishAsync(_options.CustomerEventsTopic, envelope, cancellationToken);
    }

    public async Task PublishVehicleAsync(string eventName, Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        var payload = new VehicleEventData
        {
            Id = vehicle.Id,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Year = vehicle.Year,
            RegistrationNumber = vehicle.RegistrationNumber,
            IsActive = vehicle.IsActive,
            CustomerId = vehicle.CustomerId
        };

        var envelope = new PulsarEventEnvelope<VehicleEventData>
        {
            EventName = eventName,
            OccurredAtUtc = DateTime.UtcNow,
            Payload = payload
        };

        await PublishAsync(_options.VehicleEventsTopic, envelope, cancellationToken);
    }

    private async Task PublishAsync<T>(string topic, PulsarEventEnvelope<T> envelope, CancellationToken cancellationToken)
    {
        var producer = await GetProducerAsync(topic, cancellationToken);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);

        await producer.Send(bytes, cancellationToken);

        _logger.LogInformation(
            "Published Pulsar event. Topic={Topic} EventName={EventName}",
            topic,
            envelope.EventName);
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

    public async ValueTask DisposeAsync()
    {
        foreach (var producer in _producers.Values)
            await producer.DisposeAsync();

        await _client.DisposeAsync();
        _producerLock.Dispose();
    }
}