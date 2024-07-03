using HomeChat.Backend.Chats;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HomeChat.Backend;

public static class ChatMappingExtensions
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        app.MapGet("/api/{sessionId:guid}/Prompt", HandlePrompt);

        app.MapDelete("/api/Sessions", async ([FromQuery] Guid sessionId, [FromServices] IChatSessionManager sessionManager) =>
        {
            await sessionManager.DeleteSession(sessionId);
        });
    }

    private static async Task<EmptyHttpResult> HandlePrompt(
        [FromQuery] string prompt,
        [FromQuery] int maxTokens,
        [FromServices] ILogger<Program> logger,
        [FromServices] IChatResponseWriter chatResponseWriter,
        [FromServices] IChatSessionManager sessionManager,
        HttpContext context,
        CancellationToken cancellationToken,
        [FromRoute] Guid sessionId)
    {
        logger.LogInformation("Session Id: {SessionId} Remote IP: {RemoteIpAdress} Prompt :{Prompt}", sessionId, context.Connection.RemoteIpAddress, prompt);
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        var wholeResponse = "";
        var session = await sessionManager.GetOrSetSession(sessionId);

        await session.Process(prompt, maxTokens, onNewText: async (text) =>
        {
            if (string.IsNullOrEmpty(text))
                return;
            Console.Write(text);
            wholeResponse += text;
            await chatResponseWriter.Write(text);
        }, cancellationToken);

        logger.LogInformation("Session Id: {SessionId} Ai response: {Response}", sessionId, wholeResponse);
        await chatResponseWriter.Write("");

        return TypedResults.Empty;
    }
}