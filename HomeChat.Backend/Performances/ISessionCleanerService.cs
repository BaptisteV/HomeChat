
namespace HomeChat.Backend.Performances
{
    public interface ISessionCleanerService
    {
        Task DeleteInactiveSessions();
        Task DeleteMostInactiveSession();
        Task DeleteSessionForRam(long freeRamTargetInMb);
    }
}