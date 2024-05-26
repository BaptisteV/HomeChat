using HomeChat.Common;
using Toolbelt.Blazor.SpeechSynthesis;
namespace HomeChat.SpeakerService.Crossplatform;
/*
public class SpeakerService : ISpeakerService
{
    private SpeechSynthesisUtterance _utterance;
    public IReadOnlyCollection<SpeechSynthesisVoice> Voices { get; }
    public SpeakerService(SpeechSynthesis speechSynthesis)
    {
        Setup(voiceProfile);
    }

    public async Task Say(string text)
    {

        await SpeechSynthesis.SpeakAsync(_utterance);
    }

    public void Setup(VoiceProfile voiceProfile)
    {
        _utterance = new SpeechSynthesisUtterance
        {
            Voice = this.Voices.FirstOrDefault(v => v.Name.Contains("Haruka")),
            Lang = "fr",
        };
    }
}
*/