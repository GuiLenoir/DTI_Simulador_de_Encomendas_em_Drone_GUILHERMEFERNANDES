using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.CustomerName).HasMaxLength(120).IsRequired();
        builder.Property(order => order.DestinationX).HasPrecision(10, 2);
        builder.Property(order => order.DestinationY).HasPrecision(10, 2);
        builder.Property(order => order.PackageWeightKg).HasPrecision(10, 2);
        builder.Property(order => order.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.QueueStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(order => order.QueueStatus);
        builder.HasIndex(order => order.QueuedAtUtc);
        builder.HasOne(order => order.Delivery)
            .WithOne(delivery => delivery.Order)
            .HasForeignKey<Delivery>(delivery => delivery.OrderId);
    }
}
