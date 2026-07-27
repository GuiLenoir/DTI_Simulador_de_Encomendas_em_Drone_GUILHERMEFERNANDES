using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Data;

public sealed class DroneDeliveryDbContext : DbContext
{
    public DroneDeliveryDbContext(DbContextOptions<DroneDeliveryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Drone> Drones => Set<Drone>();
    public DbSet<DeliveryOrder> Orders => Set<DeliveryOrder>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripOrder> TripOrders => Set<TripOrder>();
    public DbSet<NoFlyZone> NoFlyZones => Set<NoFlyZone>();
    public DbSet<NoFlyZonePoint> NoFlyZonePoints => Set<NoFlyZonePoint>();
    public DbSet<DroneSettings> DroneSettings => Set<DroneSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DroneDeliveryDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Drone drone)
            {
                if (entry.State == EntityState.Added)
                {
                    drone.CreatedAt = now;
                }
                drone.UpdatedAt = now;
            }

            if (entry.Entity is DeliveryOrder order)
            {
                if (entry.State == EntityState.Added)
                {
                    order.CreatedAt = now;
                }
                order.UpdatedAt = now;
            }

            if (entry.Entity is NoFlyZone zone)
            {
                if (entry.State == EntityState.Added)
                {
                    zone.CreatedAtUtc = now;
                }
                zone.UpdatedAtUtc = now;
            }

            if (entry.Entity is DroneSettings settings)
            {
                if (entry.State == EntityState.Added)
                {
                    settings.CreatedAtUtc = now;
                }
                settings.UpdatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
