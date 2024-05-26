using HomeChat.Common;
using System.Globalization;
using System.Speech.Synthesis;
namespace HomeChat.SpeakerService.Win;

public class WinSpeakerService : ISpeakerService
{
    private readonly SpeechSynthesizer _synthesizer;
    public WinSpeakerService(VoiceProfile voiceProfile)
    {
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SetOutputToDefaultAudioDevice();
        _synthesizer.Rate = 2;
        Setup(voiceProfile);
    }

    private async Task BlockingSay(string text)
    {
        var prompt = _synthesizer.SpeakAsync(text);
        while (!prompt.IsCompleted)
        {
            // TODO oO
            await Task.Delay(10);
        }
    }

    public Task Say(string text)
    {
        return BlockingSay(text);
    }

    public void Setup(VoiceProfile voiceProfile)
    {
        switch (voiceProfile)
        {
            case VoiceProfile.Ai:
                _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Senior, 0, CultureInfo.GetCultureInfo("fr-FR"));
                break;
            case VoiceProfile.User:
                _synthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Teen, 0, CultureInfo.GetCultureInfo("fr-FR"));
                break;
            default:
                throw new Exception("Voice profile not supported");
        }
    }
}