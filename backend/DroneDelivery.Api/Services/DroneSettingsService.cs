using DroneDelivery.Api.Data;
using DroneDelivery.Api.DTOs;
using DroneDelivery.Api.Exceptions;
using DroneDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DroneDelivery.Api.Services;

public sealed class DroneSettingsService : IDroneSettingsService
{
    private const int SettingsId = 1;
    private readonly DroneDeliveryDbContext _dbContext;

    public DroneSettingsService(DroneDeliveryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DroneSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<DroneSettingsResponse> UpdateAsync(UpdateDroneSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request.BatterySafetyMarginPercentagePoints is < 0 or > 30)
        {
            throw new ValidationException("GLOBAL_SAFETY_MARGIN_INVALID", "Invalid global safety margin", "Global safety margin must be between 0 and 30 percentage points.");
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.BatterySafetyMarginPercentagePoints = request.BatterySafetyMarginPercentagePoints;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    private async Task<DroneSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.DroneSettings.FirstOrDefaultAsync(item => item.Id == SettingsId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new DroneSettings { Id = SettingsId, BatterySafetyMarginPercentagePoints = 5m };
        _dbContext.DroneSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static DroneSettingsResponse Map(DroneSettings settings) =>
        new(settings.BatterySafetyMarginPercentagePoints, settings.UpdatedAtUtc);
}
