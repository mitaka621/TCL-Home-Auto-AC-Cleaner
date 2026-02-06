using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCL_Home_Auto_AC_Cleaner.Data;
using TCL_Home_Auto_AC_Cleaner.Enums;
using TCL_Home_Auto_AC_Cleaner.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var useDatabase = builder.Configuration.GetValue<bool>("UseDatabase", false);

if (useDatabase)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection not found in user secrets when UseDatabase is enabled");

    builder.Services.AddDbContext<TclHomeDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddHttpClient<TclAcService>(client =>
{
    client.DefaultRequestHeaders.Add("user-agent", "Android");
});
builder.Services.AddScoped<TclAcService>();
builder.Services.AddSingleton(serviceProvider =>
    new GlobalExceptionHandler(serviceProvider, useDatabase));
builder.Services.AddHostedService<ScheduledCleaningService>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();
var exceptionHandler = host.Services.GetRequiredService<GlobalExceptionHandler>();

if (useDatabase)
{
    try
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TclHomeDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        await exceptionHandler.HandleExceptionAsync(ex, "DatabaseInitialization");
        return;
    }
}

// Run initial cleaning immediately
var service = host.Services.GetRequiredService<TclAcService>();

try
{
    await service.AuthenticateAsync();
    var devices = await service.GetDevicesAsync();

    Console.WriteLine($"Found {devices.Count} device(s):\n");
    Console.WriteLine(string.Join($"{Environment.NewLine}================================={Environment.NewLine}", devices.Values));
    Console.WriteLine();

    if (devices.Any(x => x.Value.IsOnline == OnlineStatusEnum.Online))
    {
        var deviceIdsToClean = devices.Where(d => d.Value.IsOnline == OnlineStatusEnum.Online).Select(d => d.Key).ToList();
        Console.WriteLine($"Sending clean commands to {deviceIdsToClean.Count} online device(s)...\n");
        await service.CleanAcsAsync(deviceIdsToClean);
    }
    else
    {
        Console.WriteLine("No online devices found.");
    }
}
catch (Exception ex)
{
    await exceptionHandler.HandleExceptionAsync(ex, "Main");
}

// Start the background service and keep the application running
Console.WriteLine("\nBackground service is running. Scheduled cleaning will execute every Sunday at 10:00 AM.");
Console.WriteLine("Press Ctrl+C to stop the application.\n");

await host.RunAsync();
