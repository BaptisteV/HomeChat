using HomeChat.Common;
using System.Globalization;
using System.Speech.Synthesis;

namespace HomeChat.Speaker;

public enum VoiceSpeed
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/*
public class SpeakerService : ISpeakerService
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _muted;
    public SpeakerService(VoiceGender voiceGender, VoiceAge voiceAge, CultureInfo cultureInfo)
    {
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SetOutputToDefaultAudioDevice();
        _synthesizer.Rate = 2;
        _synthesizer.SelectVoiceByHints(voiceGender, voiceAge, 0, cultureInfo);
        _muted = false;
    }

    public async Task BlockingSay(string text)
    {
        var prompt = _synthesizer.SpeakAsync(text);
        while (!prompt.IsCompleted)
        {
            // TODO oO
            await Task.Delay(50);
        }
    }

    public void ChangeSpeed(VoiceSpeed speed)
    {
        // Can go from -10 to 10
        _synthesizer.Rate = (int)speed;
    }

    public void ChangeVoice(VoiceGender voiceGender, VoiceAge voiceAge, CultureInfo cultureInfo)
    {
        _synthesizer.SelectVoiceByHints(voiceGender, voiceAge, 0, cultureInfo);
    }

    public void Mute()
    {
        _muted = true;
    }

    public Task Say(string text)
    {
        if(_muted) return Task.CompletedTask;
        return BlockingSay(text);
    }
}*/