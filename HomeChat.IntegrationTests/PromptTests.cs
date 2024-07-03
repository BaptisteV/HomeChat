using HomeChat.Backend.AIModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace HomeChat.IntegrationTests;

[Collection("Prompt")]
public class PromptTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITestOutputHelper _testLogger;
    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _stepStopwatch;

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PromptTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _client.Timeout = TimeSpan.FromMinutes(5);
        _testLogger = testOutputHelper;
        _stopwatch = Stopwatch.StartNew();
        _stepStopwatch = Stopwatch.StartNew();
    }

    private void LogStep(string message)
    {
        _testLogger.WriteLine($"[{_stopwatch.Elapsed}] {_stepStopwatch.Elapsed} {message}");
        _stepStopwatch.Restart();
    }

    private async Task<HttpResponseMessage> Prompt(string prompt, Guid sessionId)
    {
        var queryParams = new Dictionary<string, string?>();
        queryParams.Add("prompt", prompt);
        queryParams.Add("maxTokens", 5.ToString());
        var queryString = QueryString.Create(queryParams).Value;
        return await _client.GetAsync($"api/{sessionId}/Prompt{queryString}");
    }

    private async Task Prompt(ModelDescription model)
    {
        var sessionId = Guid.NewGuid();
        var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(model.ShortName));
        await setModelResponse.AssertNoProblemDetails();
        Assert.True(setModelResponse.IsSuccessStatusCode);
        var response = await Prompt("Hi! How are you today?", sessionId);
        await response.AssertNoProblemDetails();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SimplePromptTest()
    {
        var response = await Prompt("Hi! What is your name?", Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSetModelThenPromptTest()
    {
        var sessionId = Guid.NewGuid();

        var modelsResponse = await _client.GetAsync($"api/{sessionId}/Models");
        Assert.True(modelsResponse.IsSuccessStatusCode);
        var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        var newModel = models.Find(m => !m.IsSelected);
        Assert.NotNull(newModel);

        var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(newModel.ShortName));
        Assert.True(setModelResponse.IsSuccessStatusCode);

        var response = await Prompt("Hi! How are you today?", sessionId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HundredSessions()
    {
        for (var i = 0; i < 5; i++)
        {
            var sessionId = Guid.NewGuid();
            var modelsResponse = await _client.GetAsync($"api/{sessionId}/Models");
            Assert.True(modelsResponse.IsSuccessStatusCode);
            var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
            Assert.NotNull(models);

            var lightModel = models.OrderBy(m => m.SizeInMb).ToList()[0];
            Assert.NotNull(lightModel);

            var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(lightModel.ShortName));
            Assert.True(setModelResponse.IsSuccessStatusCode);

            LogStep($"{i} {lightModel.ShortName} is set");
            var queryParams = new Dictionary<string, string?>();
            queryParams.Add("prompt", "Hi! What's up?");
            queryParams.Add("maxTokens", 10.ToString());
            var queryString = QueryString.Create(queryParams).Value;
            var response = await _client.GetAsync($"api/{sessionId}/Prompt{queryString}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LogStep($"{lightModel.ShortName} OK");
        }
    }

    [Fact(Skip = "Too long")]
    //[Fact]
    public async Task PromptAllModels()
    {
        var sessionId = Guid.NewGuid();

        var modelsResponse = await _client.GetAsync($"api/{sessionId}/Models");
        Assert.True(modelsResponse.IsSuccessStatusCode);
        var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        foreach (var model in models.OrderBy(m => m.SizeInMb))
        {
            var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(model.ShortName));
            Assert.True(setModelResponse.IsSuccessStatusCode);
            var response = await Prompt("Hi! How are you today?", sessionId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LogStep($"Prompted {model}");
        }
    }

    [Fact]
    public async Task PromptFiveModelsAtOnce()
    {
        var modelsResponse = await _client.GetAsync($"api/{Guid.NewGuid()}/Models");
        Assert.True(modelsResponse.IsSuccessStatusCode);
        var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        var fiveModels = models.OrderBy(m => m.SizeInMb).Take(3).ToList();
        var tasks = new List<Task>();
        foreach (var model in fiveModels)
        {
            tasks.Add(Prompt(model));
        }
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task PromptFiveModelsSequentially()
    {
        var modelsResponse = await _client.GetAsync($"api/{Guid.NewGuid()}/Models");
        Assert.True(modelsResponse.IsSuccessStatusCode);
        var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        foreach (var model in models.OrderBy(m => m.SizeInMb).Take(3).ToList())
        {
            await Prompt(model);
            LogStep($"Prompted {model.ShortName}");
        }
    }
}