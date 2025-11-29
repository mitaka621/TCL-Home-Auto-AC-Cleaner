using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TCL_Home_Auto_AC_Cleaner.Enums;

namespace TCL_Home_Auto_AC_Cleaner.Services;

public class ScheduledCleaningService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledCleaningService>? _logger;
    private readonly TimeSpan _scheduledTime = new TimeSpan(10, 0, 0); // 10:00 AM
    private readonly DayOfWeek _scheduledDay = DayOfWeek.Sunday;

    public ScheduledCleaningService(IServiceProvider serviceProvider, ILogger<ScheduledCleaningService>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation("Scheduled Cleaning Service started. Will run every Sunday at 10:00 AM.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRunTime = GetNextScheduledTime();
                var delay = nextRunTime - DateTime.Now;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger?.LogInformation($"Next scheduled cleaning: {nextRunTime:yyyy-MM-dd HH:mm:ss}. Waiting {delay.TotalDays:F2} days.");
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                _logger?.LogInformation("Executing scheduled cleaning...");
                await ExecuteCleaningAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in scheduled cleaning service");

                try
                {
                    var exceptionHandler = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<GlobalExceptionHandler>();
                    await exceptionHandler.HandleExceptionAsync(ex, "ScheduledCleaningService");
                }
                catch
                {
                    // If we can't log to exception handler, continue
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger?.LogInformation("Scheduled Cleaning Service stopped.");
    }

    private DateTime GetNextScheduledTime()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.Add(_scheduledTime);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        while (nextRun.DayOfWeek != _scheduledDay)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }

    private async Task ExecuteCleaningAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TclAcService>();
        var exceptionHandler = scope.ServiceProvider.GetRequiredService<GlobalExceptionHandler>();

        try
        {
            await service.AuthenticateAsync();
            var devices = await service.GetDevicesAsync();

            _logger?.LogInformation($"Found {devices.Count} device(s)");

            if (devices.Any(x => x.Value.IsOnline == OnlineStatusEnum.Online))
            {
                var deviceIdsToClean = devices
                    .Where(d => d.Value.IsOnline == OnlineStatusEnum.Online)
                    .Select(d => d.Key)
                    .ToList();

                _logger?.LogInformation($"Sending clean commands to {deviceIdsToClean.Count} online device(s)...");
                await service.CleanAcsAsync(deviceIdsToClean);
                _logger?.LogInformation("Scheduled cleaning completed successfully.");
            }
            else
            {
                _logger?.LogInformation("No online devices found for scheduled cleaning.");
            }
        }
        catch (Exception ex)
        {
            await exceptionHandler.HandleExceptionAsync(ex, "ScheduledCleaning");
            throw;
        }
    }
}

