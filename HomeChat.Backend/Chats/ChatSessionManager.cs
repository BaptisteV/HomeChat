using System.Collections.Concurrent;

namespace HomeChat.Backend.Chats;

public class ChatSessionManager : IChatSessionManager
{
    private readonly IDictionary<Guid, (SessionInfo sessionInfo, IChat chat)> _sessions = new ConcurrentDictionary<Guid, (SessionInfo sessionInfo, IChat chat)>();
    private readonly IServiceProvider _serviceCollection;
    private readonly ILogger<ChatSessionManager> _logger;

    public ChatSessionManager(IServiceProvider serviceProvider, ILogger<ChatSessionManager> logger)
    {
        _serviceCollection = serviceProvider;
        _logger = logger;
    }

    public Task DeleteSession(Guid sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
            _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }

    public async Task<IChat> GetOrSetSession(Guid sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            var textProcessor = _serviceCollection.GetService<IChat>()!;
            var selectedModel = (await textProcessor.GetModels()).First(m => m.IsSelected);
            await textProcessor.LoadSelectedModel();
            _logger.LogInformation("Starting new session {SessionId} with {SelectedModel}", sessionId, selectedModel.ShortName);

            _sessions.Add(
                sessionId,
                (sessionInfo: new SessionInfo
                {
                    Id = sessionId,
                    CreatedAt = DateTime.Now,
                    LastActivity = DateTime.Now
                },
                chat: textProcessor));
        }
        var (sessionInfo, chat) = _sessions[sessionId];
        sessionInfo.LastActivity = DateTime.Now;
        return chat;
    }

    public Task<IEnumerable<SessionInfo>> GetSessions()
    {
        return Task.FromResult(_sessions.Values.Select(v => v.sessionInfo));
    }
}