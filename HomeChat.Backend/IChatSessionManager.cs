using System.Collections.Concurrent;

namespace HomeChat.Backend;

public interface IChatSessionManager
{
    Task<ITextProcessor> GetOrSetSession(Guid sessionId);
    Task DeleteSession(Guid sessionId);
}

public class ChatSessionManager : IChatSessionManager
{
    private readonly IDictionary<Guid, ITextProcessor> _sessions = new ConcurrentDictionary<Guid, ITextProcessor>();
    private readonly IServiceProvider _serviceCollection;

    public ChatSessionManager(IServiceProvider serviceProvider)
    {
        _serviceCollection = serviceProvider;
    }

    public async Task DeleteSession(Guid sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            await _sessions[sessionId].DisposeAsync();
            _sessions.Remove(sessionId);
        }
    }

    public Task<ITextProcessor> GetOrSetSession(Guid sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            var textProcessor = _serviceCollection.GetService<ITextProcessor>()!;
            _sessions.Add(sessionId, textProcessor);
        }

        return Task.FromResult(_sessions[sessionId]);
    }
}