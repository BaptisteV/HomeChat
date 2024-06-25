using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HomeChat.IntegrationTests;

public static class HttpResponseMessageExtensions
{
    public static async Task AssertNoProblemDetails(this HttpResponseMessage message)
    {
        var content = await message.Content.ReadAsStringAsync();
        if (content == string.Empty)
        {
            Assert.True(message.IsSuccessStatusCode, $"{message.StatusCode} {message.RequestMessage?.Method} ({message.RequestMessage?.RequestUri})");
        }
        if (!message.IsSuccessStatusCode)
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content);
            if (problemDetails is null)
            {
                Assert.Fail($"{message.StatusCode} {message.RequestMessage?.Method} ({message.RequestMessage?.RequestUri}) {content}");
            }
            else
            {
                Assert.Fail($"{problemDetails.Status} {problemDetails.Title} {problemDetails.Detail}");
            }
        }
    }
}
