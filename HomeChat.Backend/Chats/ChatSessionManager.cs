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

    private async Task SetSession(Guid sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
            return;

        var chat = _serviceCollection.GetService<IChat>()!;
        var selectedModel = (await chat.GetModels()).First(m => m.IsSelected);
        await chat.LoadSelectedModel();
        _logger.LogInformation("Starting new session {SessionId} with {SelectedModel}", sessionId, selectedModel.ShortName);

        _sessions.Add(
            sessionId,
            (sessionInfo: new SessionInfo
            {
                Id = sessionId,
                CreatedAt = DateTime.Now,
                LastActivity = DateTime.Now,
                Model = selectedModel,
            },
            chat));

        var g = _sessions.Select(s => new { Id = s.Key, ModelName = s.Value.sessionInfo.Model.ShortName });
        var groupedById = g.GroupBy(g => g.Id);
        if (!groupedById.All(g => g.Count() == 1))
        {
            throw new Exception("!groupedById.All(g => g.Count() == 1)");
        }
    }

    private Task<IChat> GetSession(Guid sessionId)
    {
        var (sessionInfo, chat) = _sessions[sessionId];
        sessionInfo.LastActivity = DateTime.Now;
        return Task.FromResult(chat);
    }
    public async Task<IChat> GetOrSetSession(Guid sessionId)
    {
        await SetSession(sessionId);
        return await GetSession(sessionId);
    }

    public Task<IEnumerable<SessionInfo>> GetSessions()
    {
        return Task.FromResult(_sessions.Values.Select(v => v.sessionInfo));
    }
}