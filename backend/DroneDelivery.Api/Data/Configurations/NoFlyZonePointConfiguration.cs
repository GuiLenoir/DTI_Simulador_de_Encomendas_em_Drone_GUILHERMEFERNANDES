using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class NoFlyZonePointConfiguration : IEntityTypeConfiguration<NoFlyZonePoint>
{
    public void Configure(EntityTypeBuilder<NoFlyZonePoint> builder)
    {
        builder.Property(point => point.X)
            .HasPrecision(10, 2);

        builder.Property(point => point.Y)
            .HasPrecision(10, 2);

        builder.HasIndex(point => new { point.NoFlyZoneId, point.Sequence })
            .IsUnique();
    }
}
