using HomeChat.Client.Http;
using HomeChat.Common;
using HomeChat.Speaker;
using System.Globalization;
using System.Speech.Synthesis;
using HomeChat.SpeakerService.Win;

namespace HomeChat.Client;

public static class Program
{
    /*
    static async Task Main(string[] args)
    {
        var client = new HomeChatClient();
        Console.WriteLine("Bonjour !");
        Console.WriteLine("Lire la réponse à voix haute ? (o/n)");
        var enableSpeach = Console.ReadKey().KeyChar.ToString().ToLower() switch
        {
            "o" or "y" => true,
            "n" => false,
            _ => false,
        };

        var speaker = new SpeakerService(VoiceGender.Neutral, VoiceAge.Senior, new CultureInfo("fr-FR"));
        if (!enableSpeach)
        {
            speaker.Mute();
        }

        while (true)
        {
            var prompt = Console.ReadLine();
            await client.Prompt(prompt, async t => await speaker.Say(t));
        }
        
    }*/

    static async Task Main(string[] args)
    {
        var chatService = new ChatService(new SpeakerService.Win.WinSpeakerService(VoiceProfile.Ai), new SpeakerService.Win.WinSpeakerService(VoiceProfile.User));
        Console.WriteLine("Bonjour !");
        Console.WriteLine("Lire la réponse à voix haute ? (o/n)");
        var enableSpeach = Console.ReadLine()[0..1].ToLower() switch
        {
            "o" or "y" => true,
            "n" => false,
            _ => false,
        };
        await chatService.StartNewSession();
        while (true)
        {
            Console.WriteLine("Prompt : ");
            var prompt = Console.ReadLine();
            if (prompt == "1") prompt = "Write me a short and funny joke.";
            if (prompt == "restart" || prompt == "start") await chatService.StartNewSession();

            var fullAnswer = await chatService.Prompt(prompt);
            Console.WriteLine(fullAnswer);
        }
    }
}
