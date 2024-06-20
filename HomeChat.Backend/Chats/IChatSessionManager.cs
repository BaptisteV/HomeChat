namespace HomeChat.Backend.Chats;

public interface IChatSessionManager
{
    Task<IChat> GetOrSetSession(Guid sessionId);
    Task DeleteSession(Guid sessionId);
    Task<IEnumerable<SessionInfo>> GetSessions();
}
