using System.Text.Json;

namespace HomeChat.Backend.Performances;

public interface IPerformanceSummaryWriter
{
    Task Write(PerformanceSummary performanceSummary);
}

public class PerformanceSummaryWriter : IPerformanceSummaryWriter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PerformanceSummaryWriter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Write(PerformanceSummary performanceSummary)
    {
        var newSummary =
            $"""
            event: performanceSummary
            data: {JsonSerializer.Serialize(performanceSummary)}
            """;
        await _httpContextAccessor.HttpContext!.Response.WriteAsync(newSummary);
        await _httpContextAccessor.HttpContext.Response.WriteAsync($"\n\n");
        await _httpContextAccessor.HttpContext.Response.Body.FlushAsync();

    }
}