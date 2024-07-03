using HomeChat.Backend.Performances;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Net;

namespace HomeChat.IntegrationTests;

public class PerformanceSummaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public PerformanceSummaryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static List<PerformanceSummary> Read(string body)
    {
        var lines = body.Split('\n');
        var dataLines = lines.Where(line => line.StartsWith("data")).ToList();

        var objs = dataLines.Select(l => l.Replace("data: ", ""));

        var result = new List<PerformanceSummary>();
        foreach (var objStr in objs)
        {
            var obj = JsonConvert.DeserializeObject<PerformanceSummary>(objStr);
            result.Add(obj);
        }
        return result;
    }

    [Fact]
    public async Task PerformanceSummaryReturnsCoherentValues()
    {
        const int nReport = 10;
        var response = await _client.GetAsync($"/api/PerformanceSummary?stopAfter={nReport}&interval={10}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
        var summaries = Read(responseContent);
        Assert.NotEmpty(summaries);
        Assert.Equal(nReport, summaries.Count);
        Assert.All(summaries, s =>
        {
            Assert.InRange(s.Cpu.PercentUsed, 1, 100);
            Assert.InRange(s.Gpu.PercentUsed, 0, 100);
            Assert.InRange(s.Ram.PercentUsed, 1, 100);
            Assert.True(s.Ram.Free > 0);
            Assert.True(s.Ram.Available > 0);
        });
    }
}