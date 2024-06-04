using HomeChat.Backend;
using HomeChat.Backend.AIModels;
using HomeChat.Backend.Performances;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using NReco.Logging.File;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.All;
    o.RequestHeaders.Add(HeaderNames.ContentType);
    o.RequestHeaders.Add(HeaderNames.ContentEncoding);
    o.RequestHeaders.Add(HeaderNames.ContentLength);
    o.MediaTypeOptions.AddText("application/json");
    o.RequestBodyLogLimit = 4096;
    o.ResponseBodyLogLimit = 4096;
});

var logFile = Path.Combine(Path.GetTempPath(), "HomeChat", $"{DateTime.Now:yyyyddMMHHmmssffftt}.log");
Console.WriteLine($"Log file: {logFile}");
builder.Services.AddLogging(builder
    => builder
        .AddConsole()
        .AddFile(Path.Combine(Path.GetTempPath(), "HomeChat", $"{DateTime.Now:yyyyddMMHHmmssffftt}.log"),
            configure: o =>
            {
                o.Append = true;
                o.MinLevel = LogLevel.Debug;
                o.HandleFileError = (err) =>
                {
                    err.UseNewLogFileName($"{Path.GetFileNameWithoutExtension(err.LogFileName)}_alt{Random.Shared.Next()}{Path.GetExtension(err.LogFileName)}");
                };
            }
        )
);

builder.Services.AddTransient<IModelCollection, ModelCollection>();
builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
builder.Services.AddSingleton<IChatSessionManager, ChatSessionManager>();
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChatResponseWriter, ChatResponseWriter>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseHttpLogging();


app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

app.MapGet("/api/{sessionId:guid}/Prompt", HandlePrompt);

app.MapGet("/api/PerformanceSummary", async ([FromServices] IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

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

async Task<EmptyHttpResult> HandlePrompt(
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
            await chatResponseWriter.WriteEvent(text);
        }, cancellationToken);

    logger.LogInformation("Session Id: {SessionId} Ai response: {Response}", sessionId, wholeResponse);
    await chatResponseWriter.WriteEvent("");

    return TypedResults.Empty;
}
async Task HandleHome(HttpContext httpContext)
{
    httpContext.Response.ContentType = "text/html";
    var html = await File.ReadAllTextAsync(@"./wwwroot/index.html");
    await httpContext.Response.WriteAsync(html);
}

await app.RunAsync();

public partial class Program { protected Program() { } }