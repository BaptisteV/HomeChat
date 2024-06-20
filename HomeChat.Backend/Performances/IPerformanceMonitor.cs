namespace HomeChat.Backend.Performances;

public interface IPerformanceMonitor
{
    Task<PerformanceSummary> GetPerformanceSummaryAsync();
    Task DeleteInactiveSessions();
}
