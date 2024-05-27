using HomeChat.Backend.AIModels;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

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
        var response = await _client.GetAsync($"/api/Models");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var models = await response.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.NotNull(models);
        Assert.True(models.Count >= 2);
        Assert.Single(models, m => m.IsSelected);
    }

    [Fact]
    public async Task CanSelectAnotherModel()
    {
        var modelsResponseBefore = await _client.GetAsync($"/api/Models");
        Assert.Equal(HttpStatusCode.OK, modelsResponseBefore.StatusCode);
        var modelsBefore = await modelsResponseBefore.Content.ReadFromJsonAsync<List<ModelDescription>>();
        var newModelName = modelsBefore.First(m => !m.IsSelected).ShortName;
        var response = await _client.PostAsync($"/api/Models", JsonContent.Create(new ModelChange(newModelName)));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var modelsResponseAfter = await _client.GetAsync($"/api/Models");
        Assert.Equal(HttpStatusCode.OK, modelsResponseAfter.StatusCode);
        var modelsAfter = await modelsResponseAfter.Content.ReadFromJsonAsync<List<ModelDescription>>();
        Assert.Equal(newModelName, modelsAfter.Single(m => m.IsSelected).ShortName);
        Assert.NotEqual(modelsBefore.Single(m => m.IsSelected).ShortName, modelsAfter.Single(m => m.IsSelected).ShortName);
    }
}