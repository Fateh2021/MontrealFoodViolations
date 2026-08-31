using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MontrealFoodViolations.Application.Interfaces;
using MontrealFoodViolations.Application.Options;

namespace MontrealFoodViolations.Infrastructure.Services;

public sealed class ViolationSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ViolationSyncBackgroundService> _logger;
    private readonly ViolationSyncOptions _options;

    public ViolationSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ViolationSyncOptions> options,
        ILogger<ViolationSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Violation sync scheduler is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IViolationSyncService>();
                _logger.LogInformation("Starting scheduled sync.");
                await syncService.SyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled sync failed and will be retried on the next interval.");
            }

            var delay = TimeSpan.FromHours(_options.IntervalHours);
            _logger.LogInformation("Next scheduled sync in {Delay} hours.", _options.IntervalHours);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
