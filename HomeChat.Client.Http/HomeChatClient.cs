using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace HomeChat.Client.Http;

public interface IHomeChatClient
{
    Task StartSession();
    Task Prompt(string prompt, int maxTokens, Func<string, Task> onNewPhrase, Func<char, Task> onNewChar);
    IAsyncEnumerable<string> PromptEnumerable(string prompt, int maxTokens, Func<string, Task> onNewPhrase);
    IAsyncEnumerable<char> PromptChars(string prompt, int maxTokens);
    Task PromptAllChars(string prompt, int maxTokens, Func<char, Task> onNewChar);
    Task PromptAllChars(string prompt, int maxTokens, Action<char> onNewChar);
    IAsyncEnumerable<string> PerfGraph();
    Task<string> PerfGraph2();
}

public class HomeChatClient : IHomeChatClient, IDisposable
{
    private readonly HttpClient _client;

    public HomeChatClient()
    {
        _client = GetHttpClient();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public HttpClient GetHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:5049/");
        client.Timeout = TimeSpan.FromSeconds(60);
        return client;
    }

    public async IAsyncEnumerable<string> PerfGraph()
    {
        using var stream = await _client.GetStreamAsync($"perfgraph");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                yield return await reader.ReadToEndAsync();
            }
        }
    }

    public async Task<string> PerfGraph2()
    {
        var response = await _client.GetAsync($"perfgraph2");
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    /*
public async Task<string> PromptForWords(string prompt, Action<string> onNewWord)
{
   var agg = new WordAggregator(onNewWord);
   var result = await Prompt(prompt, (text) => agg.NewText(text));
   result += agg.Flush();
   return result;
}*/
    public async Task Prompt(string prompt, int maxTokens, Func<string, Task> onNewPhrase, Func<char, Task> onNewChar)
    {
        var agg = new WordAggregator(async (a) => await onNewPhrase(a));
        using var stream = await _client.GetStreamAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var newChar = (char)reader.Read();
                await onNewChar(newChar);
                agg.NewText(newChar.ToString());
            }
        }
        var end = agg.Flush();
        foreach (var c in end)
            await onNewChar(c);
        // TODO Not really...
        await onNewPhrase(end);
    }

    public async IAsyncEnumerable<char> PromptChars(string prompt, int maxTokens)
    {
        /*
        using var stream = await _client.GetStreamAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var c = (char)reader.Read();
                yield return c;
            }
        }*/
        using (var response = await _client.GetAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}"))
        {
            var stream = await response.Content.ReadAsStreamAsync();
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var c = (char)reader.Read();
                    yield return c;
                }
            }
        }
    }
    public async Task PromptAllChars(string prompt, int maxTokens, Func<char, Task> onNewChar)
    {
        /*
        using var stream = await _client.GetStreamAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var c = (char)reader.Read();
                yield return c;
            }
        }*/
        using var response = await _client.GetAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}").ConfigureAwait(false);
        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var c = (char)reader.Read();
            await onNewChar(c).ConfigureAwait(false);
        }
    }

    public async Task PromptAllChars(string prompt, int maxTokens, Action<char> onNewChar)
    {
        /*
        using var stream = await _client.GetStreamAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var c = (char)reader.Read();
                yield return c;
            }
        }*/
        using var response = await _client.GetAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var c = (char)reader.Read();
            onNewChar(c);
        }
    }

    public async IAsyncEnumerable<string> PromptEnumerable(string prompt, int maxTokens, Func<string, Task> onNewPhrase)
    {
        var agg = new WordAggregator(async (a) => await onNewPhrase(a));
        using var stream = await _client.GetStreamAsync($"prompt?prompt={prompt}&maxTokens={maxTokens}");
        using (var reader = new StreamReader(stream))
        {
            while (!reader.EndOfStream)
            {
                var newChar = (char)reader.Read();
                agg.NewText(newChar.ToString());
                yield return newChar.ToString();
            }
        }
        yield return agg.Flush();
    }

    public async Task StartSession()
    {
        using var client = GetHttpClient();
        var response = await client.PostAsync("start", null);
        if (!response.IsSuccessStatusCode) throw new Exception(await response.Content?.ReadAsStringAsync());
    }
}