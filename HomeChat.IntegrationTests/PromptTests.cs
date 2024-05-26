using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

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

    [Fact]
    public async Task Test1()
    {
        var prompt = "Coucou ça va ?";
        var queryParams = new Dictionary<string, string?>();
        queryParams.Add("prompt", prompt);
        queryParams.Add("maxTokens", 50.ToString());
        var queryString = QueryString.Create(queryParams).Value;
        var response = await _client.GetAsync($"/prompt{queryString}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}