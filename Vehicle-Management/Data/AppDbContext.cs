using Microsoft.EntityFrameworkCore;
using VehicleManagementApi.Models;

namespace VehicleManagementApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.RegistrationNumber)
            .IsUnique();
    }
}
