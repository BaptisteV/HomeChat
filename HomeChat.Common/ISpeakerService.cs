
using System.Globalization;

namespace HomeChat.Common;

public enum VoiceProfile
{
    Ai,
    User,
}

public interface ISpeakerService
{
    void Setup(VoiceProfile voiceProfile);
    Task Say(string text);
}