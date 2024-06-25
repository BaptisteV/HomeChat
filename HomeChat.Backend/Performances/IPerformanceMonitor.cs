namespace HomeChat.Backend.Performances;

public interface IPerformanceMonitor
{
    PerformanceSummary GetPerformanceSummary();
    Task DeleteInactiveSessions();
    Task DeleteSessionForRam(long freeRamTargetInMb);

    Task DeleteMostInactiveSession();
}
