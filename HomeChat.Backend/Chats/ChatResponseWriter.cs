using System.Text.Json;

namespace HomeChat.Backend.Chats;

public class ChatResponseWriter : IChatResponseWriter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatResponseWriter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task Write(string newText)
    {
        var newEvent =
            $"""
            event: aiMessage
            data: {JsonSerializer.Serialize(new { newText = newText })}
            """;
        await _httpContextAccessor.HttpContext!.Response.WriteAsync(newEvent);
        await _httpContextAccessor.HttpContext.Response.WriteAsync($"\n\n");
        await _httpContextAccessor.HttpContext.Response.Body.FlushAsync();
    }
}