using VehicleManagementApi.Models;

namespace VehicleManagementApi.Pulsar;

public sealed class NullPulsarEventPublisher : IPulsarEventPublisher
{
    public Task PublishCustomerAsync(string eventName, Customer customer, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishVehicleAsync(string eventName, Vehicle vehicle, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}