using HomeChat.Backend.AIModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomeChat.IntegrationTests;

public class ModelSelectionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    public ModelSelectionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AtLeast2Models()
    {
        var response = await _client.GetAsync($"/api/{Guid.NewGuid()}/Models");
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content)!;
            Assert.Fail($"{problemDetails.Status} {problemDetails.Title} {problemDetails.Detail}");
        }
        var models = await response.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        Assert.True(models.Count >= 2);
        Assert.Single(models, m => m.IsSelected);
    }

    [Fact]
    public async Task CanSelectAnotherModel()
    {
        var sessionId = Guid.NewGuid();
        var modelsResponseBefore = await _client.GetAsync($"/api/{sessionId}/Models");
        Assert.Equal(HttpStatusCode.OK, modelsResponseBefore.StatusCode);
        var modelsBefore = (await modelsResponseBefore.Content.ReadFromJsonAsync<List<ModelDescription>>())!;
        var newModelName = modelsBefore.First(m => !m.IsSelected).ShortName!;
        var response = await _client.PostAsync($"/api/{sessionId}/Models", JsonContent.Create(new ModelChange(newModelName)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var modelsResponseAfter = await _client.GetAsync($"/api/{sessionId}/Models");
        Assert.Equal(HttpStatusCode.OK, modelsResponseAfter.StatusCode);
        var modelsAfter = (await modelsResponseAfter.Content.ReadFromJsonAsync<List<ModelDescription>>())!;
        Assert.Equal(newModelName, modelsAfter.Single(m => m.IsSelected).ShortName);
        Assert.NotEqual(modelsBefore!.Single(m => m.IsSelected).ShortName, modelsAfter.Single(m => m.IsSelected).ShortName);
    }
}