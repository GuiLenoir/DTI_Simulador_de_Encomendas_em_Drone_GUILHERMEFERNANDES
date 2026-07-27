using DroneDelivery.Api.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DroneDelivery.Tests;

public sealed class MigrationTests
{
    [Fact]
    public void InitialCreate_HasMigrationMetadata()
    {
        var migrationType = typeof(InitialCreate);

        Assert.NotNull(migrationType.GetCustomAttributes(typeof(MigrationAttribute), inherit: false).SingleOrDefault());
        Assert.NotNull(migrationType.GetCustomAttributes(typeof(DbContextAttribute), inherit: false).SingleOrDefault());
    }
}
