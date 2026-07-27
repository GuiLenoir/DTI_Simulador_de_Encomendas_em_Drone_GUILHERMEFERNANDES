using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");
        builder.HasKey(trip => trip.Id);
        builder.Property(trip => trip.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(trip => trip.TotalWeightKg).HasPrecision(10, 2);
        builder.Property(trip => trip.EstimatedDistanceKm).HasPrecision(10, 2);
        builder.Property(trip => trip.EstimatedBatteryConsumptionPercentagePoints).HasPrecision(5, 2);
        builder.Property(trip => trip.BatterySafetyMarginPercentagePoints).HasPrecision(5, 2);
        builder.Property(trip => trip.MinimumRequiredBatteryPercentage).HasPrecision(5, 2);
        builder.Property(trip => trip.BatteryAtDeparturePercentage).HasPrecision(5, 2);
        builder.Property(trip => trip.ExpectedBatteryAtReturnPercentage).HasPrecision(5, 2);
        builder.HasIndex(trip => trip.Status);
        builder.HasIndex(trip => trip.DroneId);
        builder.HasOne(trip => trip.Drone)
            .WithMany(drone => drone.Trips)
            .HasForeignKey(trip => trip.DroneId);
    }
}
