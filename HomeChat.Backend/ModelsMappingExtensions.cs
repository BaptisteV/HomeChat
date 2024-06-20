using HomeChat.Backend.AIModels;
using HomeChat.Backend.Chats;
using Microsoft.AspNetCore.Mvc;

namespace HomeChat.Backend;

public static class ModelsMappingExtensions
{
    public static void MapModelsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/{sessionId:guid}/Models",
            async ([FromBody] ModelChange modelShortName,
            HttpContext context,
            [FromServices] IChatSessionManager sessionManager,
            [FromServices] ILogger<Program> logger,
            [FromRoute] Guid sessionId) =>
            {
                var session = await sessionManager.GetOrSetSession(sessionId);
                logger.LogInformation("Session Id: {SessionId} Remote IP: {RemoteIpAdress} New model short name: {NewModelShortName}", sessionId, context.Connection.RemoteIpAddress, modelShortName.NewModelShortName);
                await session.SelectModel(modelShortName.NewModelShortName);
                await session.LoadSelectedModel();
            });

        app.MapGet("/api/{sessionId:guid}/Models",
            async (HttpContext context,
            [FromServices] IChatSessionManager sessionManager,
            [FromRoute] Guid sessionId,
            [FromServices] ILogger<Program> logger) =>
            {
                var session = await sessionManager.GetOrSetSession(sessionId);
                var models = await session.GetModels();
                var selectedModel = models.Single(m => m.IsSelected);
                logger.LogInformation("Session Id: {SessionId} Remote IP: {RemoteIpAdress} Retreived {ModelsCount}. Selected model : {SelectedModel}", sessionId, context.Connection.RemoteIpAddress, models.Count, selectedModel.ShortName);
                return models;
            });
    }

}
