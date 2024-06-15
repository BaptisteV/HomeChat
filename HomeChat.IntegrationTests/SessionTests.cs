using Microsoft.AspNetCore.Mvc.Testing;

namespace HomeChat.IntegrationTests;


public class SessionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    public SessionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    [Fact]
    public async Task DeleteSessionTest()
    {
        var sessionId = Guid.NewGuid();
        _ = await _client.GetAsync($"/api/{sessionId}/Models");

        var deleteResponse = await _client.DeleteAsync($"/api/Sessions?sessionId={sessionId}");
        Assert.NotNull(deleteResponse);
        Assert.True(deleteResponse.IsSuccessStatusCode);
    }
}