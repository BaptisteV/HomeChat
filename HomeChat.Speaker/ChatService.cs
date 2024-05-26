using HomeChat.Client.Http;
using HomeChat.Common;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;

namespace HomeChat.Speaker;

public class ChatService : IChatters
{
    public Chatter Ai { get; init; }
    public Chatter User { get; init; }

    IChatter IChatters.Ai => Ai;

    IChatter IChatters.User => User;

    private readonly HomeChatClient _homeChatClient;

    public ChatService(ISpeakerService aiSpeakerService, ISpeakerService userSpeakerService)
    {
        Ai = new Chatter(aiSpeakerService);
        User = new Chatter(userSpeakerService);
        _homeChatClient = new HomeChatClient();
    }

    public void MuteAi() => Ai.Bypassed = true;
    public void MuteUser() => User.Bypassed = true;

    public void UnmuteAi() => Ai.Bypassed = false;
    public void UnmuteUser() => User.Bypassed = false;

    public async Task StartNewSession()
    {
        await _homeChatClient.StartSession();
    }

    public async Task<string> Prompt(string prompt)
    {
        if (!User.Bypassed)
            await User.Say(prompt);

        var fullPrompt = await _homeChatClient.Prompt(prompt, c => Console.Write(c), async t => await Ai.Say(t));

        return fullPrompt;
    }
}
