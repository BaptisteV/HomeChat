namespace HomeChat.Backend.Performances;

public class InactiveSessionCleanerService(
    ISessionCleanerService _sessionCleanerService,
    IPerformanceMonitor _performanceMonitor,
    ILogger<InactiveSessionCleanerService> _logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(200);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var perf = _performanceMonitor.GetPerformanceSummary();
            if (perf.Ram.PercentUsed >= 95)
            {
                _logger.LogWarning("{Source} trigged because RAM usage is {RamPercentUsed}%", nameof(InactiveSessionCleanerService), perf.Ram.PercentUsed);
                await _sessionCleanerService.DeleteMostInactiveSession();
            }
        }
    }
}
