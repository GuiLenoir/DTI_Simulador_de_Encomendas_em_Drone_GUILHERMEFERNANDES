using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneDelivery.Api.Data.Configurations;

public sealed class DroneSettingsConfiguration : IEntityTypeConfiguration<DroneSettings>
{
    public void Configure(EntityTypeBuilder<DroneSettings> builder)
    {
        builder.ToTable("DroneSettings");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.BatterySafetyMarginPercentagePoints).HasPrecision(5, 2);

        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(new DroneSettings
        {
            Id = 1,
            BatterySafetyMarginPercentagePoints = 5m,
            CreatedAtUtc = seedTime,
            UpdatedAtUtc = seedTime
        });
    }
}
