namespace HomeChat.Backend;

public interface IChatSessionManager
{
    Task<IChat> GetOrSetSession(Guid sessionId);
    Task DeleteSession(Guid sessionId);
}
