using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class NoFlyZoneConfiguration : IEntityTypeConfiguration<NoFlyZone>
{
    public void Configure(EntityTypeBuilder<NoFlyZone> builder)
    {
        builder.Property(zone => zone.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(zone => zone.CreatedAtUtc)
            .IsRequired();

        builder.Property(zone => zone.UpdatedAtUtc)
            .IsRequired();

        builder.HasMany(zone => zone.Points)
            .WithOne(point => point.NoFlyZone)
            .HasForeignKey(point => point.NoFlyZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
