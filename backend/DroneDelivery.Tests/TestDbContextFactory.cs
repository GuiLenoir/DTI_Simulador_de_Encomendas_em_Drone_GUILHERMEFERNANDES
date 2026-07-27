using DroneDelivery.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DroneDelivery.Tests;

internal static class TestDbContextFactory
{
    public static DroneDeliveryDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DroneDeliveryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new DroneDeliveryDbContext(options);
    }
}
