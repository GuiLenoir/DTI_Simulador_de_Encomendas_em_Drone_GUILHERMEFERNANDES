using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class TripOrderConfiguration : IEntityTypeConfiguration<TripOrder>
{
    public void Configure(EntityTypeBuilder<TripOrder> builder)
    {
        builder.ToTable("TripOrders");
        builder.HasKey(tripOrder => new { tripOrder.TripId, tripOrder.OrderId });
        builder.HasIndex(tripOrder => tripOrder.OrderId);
        builder.HasIndex(tripOrder => new { tripOrder.TripId, tripOrder.DeliverySequence }).IsUnique();
        builder.HasOne(tripOrder => tripOrder.Trip)
            .WithMany(trip => trip.TripOrders)
            .HasForeignKey(tripOrder => tripOrder.TripId);
        builder.HasOne(tripOrder => tripOrder.Order)
            .WithMany(order => order.TripOrders)
            .HasForeignKey(tripOrder => tripOrder.OrderId);
    }
}
