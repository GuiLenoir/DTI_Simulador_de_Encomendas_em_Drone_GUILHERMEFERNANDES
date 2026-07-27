using DroneDelivery.Api.Data;
using DroneDelivery.Api.Middlewares;
using DroneDelivery.Api.Options;
using DroneDelivery.Api.Services;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is missing.");

builder.Services.Configure<DroneDeliveryOptions>(builder.Configuration.GetSection("DroneDelivery"));
builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
builder.Services.AddDbContext<DroneDeliveryDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 4, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services.AddScoped<IDistanceService, DistanceService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IDeliveryStateService, DeliveryStateService>();
builder.Services.AddScoped<ITripStateService, TripStateService>();
builder.Services.AddScoped<IChargingService, ChargingService>();
builder.Services.AddScoped<IDroneStateService, DroneStateService>();
builder.Services.AddScoped<IDroneSettingsService, DroneSettingsService>();
builder.Services.AddScoped<IDroneOrderCapabilityService, DroneOrderCapabilityService>();
builder.Services.AddScoped<IDroneService, DroneService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IDeliveryPlanningService, DeliveryPlanningService>();
builder.Services.AddScoped<IUpcomingTripService, UpcomingTripService>();
builder.Services.AddScoped<IRoutePlanningService, RoutePlanningService>();
builder.Services.AddScoped<INoFlyZoneService, NoFlyZoneService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICustomerSimulationService, CustomerSimulationService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
    }

    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    var dbContext = scope.ServiceProvider.GetRequiredService<DroneDeliveryDbContext>();
    await ApplyMigrationsAsync(dbContext, logger);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

await app.RunAsync();

static async Task ApplyMigrationsAsync(DroneDeliveryDbContext dbContext, ILogger logger)
{
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            logger.LogWarning(exception, "Database migration attempt {Attempt} failed. Retrying in 3 seconds.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    await dbContext.Database.MigrateAsync();
}

public partial class Program;
