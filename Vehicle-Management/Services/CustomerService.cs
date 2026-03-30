using Microsoft.Extensions.Caching.Memory;
using VehicleManagementApi.Caching;
using VehicleManagementApi.Models;
using VehicleManagementApi.Pulsar;
using VehicleManagementApi.Repositories;

namespace VehicleManagementApi.Services;

public class CustomerService : ICustomerService
{
    private static readonly TimeSpan AllCustomersCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CustomerByIdCacheDuration = TimeSpan.FromMinutes(3);

    private readonly ICustomerRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CustomerService> _logger;
    private readonly IPulsarEventPublisher _pulsarPublisher;

    public CustomerService(
        ICustomerRepository repo,
        IMemoryCache cache,
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
        if (_cache.TryGetValue(CacheKeys.Customers.All, out List<Customer>? cachedCustomers) &&
            cachedCustomers is not null)
        {
            _logger.LogInformation("CACHE HIT - Customers");
            return cachedCustomers;
        }

        _logger.LogInformation("CACHE MISS - Customers (DB call)");

        var customers = await _repo.GetAllAsync();

        _cache.Set(
            CacheKeys.Customers.All,
            customers,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AllCustomersCacheDuration
            });

        return customers;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        var cacheKey = CacheKeys.Customers.ById(id);

        if (_cache.TryGetValue(cacheKey, out Customer? cachedCustomer))
        {
            _logger.LogInformation("CACHE HIT - Customer Id={Id}", id);
            return cachedCustomer;
        }

        _logger.LogInformation("CACHE MISS - Customer Id={Id} (DB call)", id);

        var customer = await _repo.GetByIdAsync(id);

        if (customer is not null)
        {
            _cache.Set(
                cacheKey,
                customer,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CustomerByIdCacheDuration
                });
        }

        return customer;
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

        InvalidateCustomerCaches(created.Id);
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

        InvalidateCustomerCaches(id);
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

        InvalidateCustomerCaches(id);
        await _pulsarPublisher.PublishCustomerAsync("customer.deleted", existing);

        _logger.LogInformation("Customer deleted successfully. Id={Id}", id);
        return true;
    }

    private void InvalidateCustomerCaches(int customerId)
    {
        _logger.LogInformation("Invalidating customer cache. CustomerId={CustomerId}", customerId);

        _cache.Remove(CacheKeys.Customers.All);
        _cache.Remove(CacheKeys.Customers.ById(customerId));
        _cache.Remove(CacheKeys.Vehicles.ByCustomerId(customerId));
    }
}