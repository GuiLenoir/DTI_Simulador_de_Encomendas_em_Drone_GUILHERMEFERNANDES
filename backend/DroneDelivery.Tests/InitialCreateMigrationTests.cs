using DroneDelivery.Api.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DroneDelivery.Tests;

public sealed class InitialCreateMigrationTests
{
    [Fact]
    public void Up_UsesExplicitColumnTypesForSeedData()
    {
        var migration = new TestableInitialCreate();
        var builder = new MigrationBuilder("MySql");

        migration.InvokeUp(builder);

        var seedOperation = builder.Operations.OfType<InsertDataOperation>().Single();

        Assert.NotNull(seedOperation.ColumnTypes);
        Assert.All(seedOperation.ColumnTypes, Assert.NotNull);
    }

    private sealed class TestableInitialCreate : InitialCreate
    {
        public void InvokeUp(MigrationBuilder migrationBuilder)
        {
            Up(migrationBuilder);
        }
    }
}
