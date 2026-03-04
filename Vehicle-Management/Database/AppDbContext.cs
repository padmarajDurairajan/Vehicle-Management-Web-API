using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.RegistrationNumber)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasQueryFilter(v => v.IsActive);
    }
}
