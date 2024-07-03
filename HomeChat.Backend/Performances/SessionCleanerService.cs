
using HomeChat.Backend.Chats;

namespace HomeChat.Backend.Performances
{
    public class SessionCleanerService : ISessionCleanerService
    {
        private readonly IChatSessionManager _chatSessionManager;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ILogger<SessionCleanerService> _logger;

        public SessionCleanerService(IChatSessionManager chatSessionManager, IPerformanceMonitor performanceMonitor, ILogger<SessionCleanerService> logger)
        {
            _chatSessionManager = chatSessionManager;
            _performanceMonitor = performanceMonitor;
            _logger = logger;
        }
        private async Task DeleteIncativeSessionsUntil(Func<bool> until)
        {
            const int MAX_RETRIES = 10;
            int retries = 0;
            do
            {
                await DeleteInactiveSessions();
                retries++;
            }
            while (until() && retries <= MAX_RETRIES);
        }

        public async Task DeleteSessionForRam(long freeRamTargetInMb)
        {
            await DeleteIncativeSessionsUntil(() =>
            {
                var summary = _performanceMonitor.GetPerformanceSummary();
                return summary.Ram.Free > freeRamTargetInMb;
            });
        }

        public List<SessionInfo> InactiveSessions(IEnumerable<SessionInfo> sessions)
        {
            var biggest = sessions
                .OrderBy(s => s.Model.SizeInMb)
                .Take(Math.Max(sessions.Count() / 4, 1));
            return biggest
                .OrderBy(s => s.LastActivity)
                .Take(Math.Max(biggest.Count() / 4, 1))
                .ToList();
        }


        public async Task DeleteMostInactiveSession()
        {
            var sessions = await _chatSessionManager.GetSessions();
            if (sessions.Count() <= 1)
            {
                return;
            }

            var mostInactiveSession = InactiveSessions(sessions)[0];
            await _chatSessionManager.DeleteSession(mostInactiveSession.Id);
        }

        public async Task DeleteInactiveSessions()
        {
            var sessions = await _chatSessionManager.GetSessions();
            if (sessions.Count() <= 1)
            {
                return;
            }

            var inactiveSessions = InactiveSessions(sessions);

            var deleteTasks = inactiveSessions.Select(s => _chatSessionManager.DeleteSession(s.Id));
            await Task.WhenAll(deleteTasks);
            _logger.LogInformation("{InactiveSessionsCount} sessions deleted", inactiveSessions.Count);
        }
    }
}
