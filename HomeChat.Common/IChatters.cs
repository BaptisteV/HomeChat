namespace HomeChat.Common;

public interface IChatters
{
    void MuteAi();
    void MuteUser();
    void UnmuteAi();
    void UnmuteUser();
    IChatter Ai { get; }
    IChatter User { get; }
    Task StartNewSession();
    Task<string> Prompt(string prompt);
}
