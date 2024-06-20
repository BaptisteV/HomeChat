using HomeChat.Backend.AIModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace HomeChat.IntegrationTests;

public class PromptTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    public PromptTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private async Task<HttpResponseMessage> Prompt(string prompt)
    {
        var queryParams = new Dictionary<string, string?>();
        queryParams.Add("prompt", prompt);
        queryParams.Add("maxTokens", 5.ToString());
        var queryString = QueryString.Create(queryParams).Value;
        return await _client.GetAsync($"api/{Guid.NewGuid()}/Prompt{queryString}");
    }

    [Fact]
    public async Task SimplePromptTest()
    {
        var response = await Prompt("Hi! What is your name?");
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

        var response = await Prompt("Hi! How are you today?");
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

            var lightModels = models.OrderBy(m => m.SizeInMb).Take(models.Count / 8).ToList();
            var newModel = lightModels.ToArray()[Random.Shared.Next(0, lightModels.Count - 1)];
            Assert.NotNull(newModel);

            var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(newModel.ShortName));
            Assert.True(setModelResponse.IsSuccessStatusCode);

            var queryParams = new Dictionary<string, string?>();
            queryParams.Add("prompt", "Hi! What's up?");
            queryParams.Add("maxTokens", 10.ToString());
            var queryString = QueryString.Create(queryParams).Value;
            var response = await _client.GetAsync($"api/{sessionId}/Prompt{queryString}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact(Skip = "Too long")]
    public async Task PromptAllModel()
    {
        var sessionId = Guid.NewGuid();

        var modelsResponse = await _client.GetAsync($"api/{sessionId}/Models");
        Assert.True(modelsResponse.IsSuccessStatusCode);
        var models = await modelsResponse.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        foreach (var model in models)
        {

            var setModelResponse = await _client.PostAsJsonAsync($"api/{sessionId}/Models", new ModelChange(model.ShortName));
            Assert.True(setModelResponse.IsSuccessStatusCode);
            var response = await Prompt("Hi! How are you today?");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}