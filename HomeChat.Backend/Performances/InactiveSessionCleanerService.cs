using HomeChat.Backend.Chats;

namespace HomeChat.Backend.Performances;

public class InactiveSessionCleanerService : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(500);
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly IChatSessionManager _chatSessionManager;

    public InactiveSessionCleanerService(IPerformanceMonitor performanceMonitor, IChatSessionManager chatSessionManager)
    {
        _performanceMonitor = performanceMonitor;
        _chatSessionManager = chatSessionManager;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
        {
            var sessionCount = (await _chatSessionManager.GetSessions()).Count();

            var perf = await _performanceMonitor.GetPerformanceSummaryAsync();
            if (perf.Ram > 75)
            {
                await _performanceMonitor.DeleteInactiveSessions();
            }
        }
    }
}
