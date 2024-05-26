using HomeChat.Common;

namespace HomeChat.Speaker;

public class Chatter : IChatter
{
    public bool Bypassed { get; set; }
    private readonly ISpeakerService _speaker;
    public enum Persona
    {
        Male,
        Female,
    }

    public Chatter(ISpeakerService speakerService)
    {
        _speaker = speakerService;
        Bypassed = false;
    }

    public async Task Say(string text)
    {
        if (Bypassed) return;
        await _speaker.Say(text);
    }
}
