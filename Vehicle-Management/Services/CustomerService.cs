using VehicleManagementApi.Caching;
using VehicleManagementApi.Models;
using VehicleManagementApi.Pulsar;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class CustomerService : ICustomerService
{
    private static readonly TimeSpan AllCustomersMemoryCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AllCustomersDistributedCacheDuration = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan CustomerByIdMemoryCacheDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan CustomerByIdDistributedCacheDuration = TimeSpan.FromMinutes(10);

    private readonly ICustomerRepository _repo;
    private readonly ICacheService _cache;
    private readonly ILogger<CustomerService> _logger;
    private readonly IPulsarEventPublisher _pulsarPublisher;

    public CustomerService(
        ICustomerRepository repo,
        ICacheService cache,
        ILogger<CustomerService> logger,
        IPulsarEventPublisher pulsarPublisher)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
        _pulsarPublisher = pulsarPublisher;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.Customers.All,
            async _ =>
            {
                _logger.LogInformation("CACHE MISS - Customers (DB call)");
                return await _repo.GetAllAsync();
            },
            AllCustomersMemoryCacheDuration,
            AllCustomersDistributedCacheDuration) ?? new List<Customer>();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.Customers.ById(id),
            async _ =>
            {
                _logger.LogInformation("CACHE MISS - Customer Id={Id} (DB call)", id);
                return await _repo.GetByIdAsync(id);
            },
            CustomerByIdMemoryCacheDuration,
            CustomerByIdDistributedCacheDuration);
    }

    public async Task<(bool Success, string? Error, Customer? Customer)> CreateAsync(Customer input)
    {
        _logger.LogInformation("Creating customer. Email={Email}", input.Email);

        var emailExists = await _repo.EmailExistsAsync(input.Email);
        if (emailExists)
        {
            _logger.LogWarning("Create customer failed: duplicate Email={Email}", input.Email);
            return (false, "Email already exists.", null);
        }

        input.Id = 0;
        input.CreatedAtUtc = DateTime.UtcNow;

        var created = await _repo.AddAsync(input);

        await InvalidateCustomerCachesAsync(created.Id);
        await _pulsarPublisher.PublishCustomerAsync("customer.created", created);

        _logger.LogInformation("Customer created successfully. Id={Id} Email={Email}", created.Id, created.Email);
        return (true, null, created);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Customer input)
    {
        _logger.LogInformation("Updating customer. Id={Id}", id);

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Update customer failed: not found. Id={Id}", id);
            return (false, "Not found.");
        }

        var emailConflict = await _repo.EmailExistsAsync(input.Email, excludeId: id);
        if (emailConflict)
        {
            _logger.LogWarning("Update customer failed: duplicate Email={Email} Id={Id}", input.Email, id);
            return (false, "Email already exists.");
        }

        existing.FullName = input.FullName;
        existing.Email = input.Email;
        existing.Phone = input.Phone;
        existing.Address = input.Address;

        await _repo.UpdateAsync(existing);

        await InvalidateCustomerCachesAsync(id);
        await _pulsarPublisher.PublishCustomerAsync("customer.updated", existing);

        _logger.LogInformation("Customer updated successfully. Id={Id}", id);
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting customer. Id={Id}", id);

        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
        {
            _logger.LogWarning("Delete customer failed: not found. Id={Id}", id);
            return false;
        }

        var deleted = await _repo.DeleteAsync(id);

        if (!deleted)
        {
            _logger.LogWarning("Delete customer failed during repository delete. Id={Id}", id);
            return false;
        }

        await InvalidateCustomerCachesAsync(id);
        await _pulsarPublisher.PublishCustomerAsync("customer.deleted", existing);

        _logger.LogInformation("Customer deleted successfully. Id={Id}", id);
        return true;
    }

    private async Task InvalidateCustomerCachesAsync(int customerId)
    {
        _logger.LogInformation("Invalidating customer cache. CustomerId={CustomerId}", customerId);

        await _cache.RemoveAsync(CacheKeys.Customers.All);
        await _cache.RemoveAsync(CacheKeys.Customers.ById(customerId));
        await _cache.RemoveAsync(CacheKeys.Vehicles.ByCustomerId(customerId));
    }
}