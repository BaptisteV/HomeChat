using HomeChat.Backend.Performances;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace HomeChat.IntegrationTests;

public class PerformanceMonitorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public PerformanceMonitorTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PerformanceSummaryReturnsCoherentValues()
    {
        var response = await _client.GetAsync($"/api/PerformanceSummary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PerformanceSummary>();
        Assert.NotNull(result);
        Assert.InRange(result.Gpu, 0, 100);
        Assert.InRange(result.Cpu, 0, 100);
        Assert.True(result.Ram > 0);
    }
}