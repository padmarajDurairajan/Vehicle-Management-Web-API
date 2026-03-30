using VehicleManagementApi.Models;

namespace VehicleManagementApi.Pulsar;

public interface IPulsarEventPublisher
{
    Task PublishCustomerAsync(string eventName, Customer customer, CancellationToken cancellationToken = default);
    Task PublishVehicleAsync(string eventName, Vehicle vehicle, CancellationToken cancellationToken = default);
}