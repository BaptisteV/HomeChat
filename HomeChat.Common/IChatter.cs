namespace HomeChat.Common;

public interface IChatter
{
    bool Bypassed { get; set; }
    Task Say(string text);
}
