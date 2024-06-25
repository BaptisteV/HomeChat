namespace HomeChat.Backend.Performances;

public interface IUsage
{
    int PercentUsed { get; }
    int Available { get; }
    int Free { get; }
}
