using System.Text.Json;
using Microsoft.Extensions.Options;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Pulsar;

public sealed class PulsarEventPublisher : IPulsarEventPublisher
{
    private readonly PulsarOptions _options;
    private readonly IPulsarPublishQueue _queue;
    private readonly ILogger<PulsarEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PulsarEventPublisher(
        IOptions<PulsarOptions> options,
        IPulsarPublishQueue queue,
        ILogger<PulsarEventPublisher> logger)
    {
        _options = options.Value;
        _queue = queue;
        _logger = logger;
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

        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);

        await _queue.EnqueueAsync(new PulsarPublishQueueItem
        {
            Topic = _options.CustomerEventsTopic,
            EventName = eventName,
            Payload = bytes
        }, cancellationToken);

        _logger.LogInformation(
            "Customer Pulsar event queued. Topic={Topic} EventName={EventName}",
            _options.CustomerEventsTopic,
            eventName);
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

        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);

        await _queue.EnqueueAsync(new PulsarPublishQueueItem
        {
            Topic = _options.VehicleEventsTopic,
            EventName = eventName,
            Payload = bytes
        }, cancellationToken);

        _logger.LogInformation(
            "Vehicle Pulsar event queued. Topic={Topic} EventName={EventName}",
            _options.VehicleEventsTopic,
            eventName);
    }
}