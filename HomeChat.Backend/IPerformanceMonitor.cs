
namespace HomeChat.Backend
{
    public interface IPerformanceMonitor
    {
        Task<PerformanceSummary> GetPerformanceSummaryAsync();
    }
}