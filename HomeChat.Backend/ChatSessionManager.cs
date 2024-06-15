using System.Collections.Concurrent;

namespace HomeChat.Backend;

public class ChatSessionManager : IChatSessionManager
{
    private readonly IDictionary<Guid, IChat> _sessions = new ConcurrentDictionary<Guid, IChat>();
    private readonly IServiceProvider _serviceCollection;
    private readonly ILogger<ChatSessionManager> _logger;

    public ChatSessionManager(IServiceProvider serviceProvider, ILogger<ChatSessionManager> logger)
    {
        _serviceCollection = serviceProvider;
        _logger = logger;
    }

    public async Task DeleteSession(Guid sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            await _sessions[sessionId].DisposeAsync();
            _sessions.Remove(sessionId);
        }
    }

    public async Task<IEnumerable<(string modelShortName, string[] conversation)>> GetConversations()
    {
        var conversations = new List<(string modelShortName, string[] conversation)>();
        foreach (var session in _sessions.Values)
        {
            conversations.Add(await session.GetConversation());
        }
        return conversations;
    }

    public async Task<IChat> GetOrSetSession(Guid sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            var textProcessor = _serviceCollection.GetService<IChat>()!;
            var selectedModel = (await textProcessor.GetModels()).First(m => m.IsSelected);
            await textProcessor.LoadSelectedModel();
            _logger.LogInformation("Starting new session {SessionId} with {SelectedModel}", sessionId, selectedModel.ShortName);
            _sessions.Add(sessionId, textProcessor);
        }

        return _sessions[sessionId];
    }
}