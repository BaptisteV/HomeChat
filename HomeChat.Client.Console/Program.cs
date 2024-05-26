using HomeChat.Client.Http;

Console.ReadLine();
const string prompt = "invente les paroles d'une chanson sur la mort et le diable, en français.";
var client = new HomeChatClient();
await client.StartSession();
while (true)
{
    /*
    await client.PromptAllChars(prompt, 50, (c) => 
    { 
        Console.Write(c);
    });
    */
    var res = client.PromptEnumerable(prompt, 50, (phrase) => {
        Console.WriteLine($"NEW PHRASE : {phrase}");
        return Task.CompletedTask;
    }
    );
    await foreach(var c in res)
    {
        Console.Write(c);
    }
    Console.ReadLine();
}