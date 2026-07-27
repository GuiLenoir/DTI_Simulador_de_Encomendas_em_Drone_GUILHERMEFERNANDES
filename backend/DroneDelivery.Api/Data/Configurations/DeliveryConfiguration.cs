using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(delivery => delivery.Id);
        builder.Property(delivery => delivery.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(delivery => delivery.StartX).HasPrecision(10, 2);
        builder.Property(delivery => delivery.StartY).HasPrecision(10, 2);
        builder.Property(delivery => delivery.DestinationX).HasPrecision(10, 2);
        builder.Property(delivery => delivery.DestinationY).HasPrecision(10, 2);
        builder.Property(delivery => delivery.EndX).HasPrecision(10, 2);
        builder.Property(delivery => delivery.EndY).HasPrecision(10, 2);
        builder.Property(delivery => delivery.EstimatedDistanceKm).HasPrecision(10, 2);
        builder.Property(delivery => delivery.EstimatedBatteryConsumptionPercent).HasPrecision(5, 2);
        builder.Property(delivery => delivery.EstimatedDurationMinutes).HasPrecision(10, 2);
        builder.Property(delivery => delivery.CreatedAtUtc).IsRequired();
        builder.Property(delivery => delivery.LoadingStartedAtUtc).IsRequired();
        builder.Property(delivery => delivery.FlyingStartedAtUtc).IsRequired();
        builder.Property(delivery => delivery.DeliveringStartedAtUtc).IsRequired();
        builder.Property(delivery => delivery.ReturningStartedAtUtc).IsRequired();
        builder.Property(delivery => delivery.CompletedAtUtc).IsRequired();
        builder.HasOne(delivery => delivery.Drone)
            .WithMany(drone => drone.Deliveries)
            .HasForeignKey(delivery => delivery.DroneId);
    }
}
