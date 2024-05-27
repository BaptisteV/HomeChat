using HomeChat.Backend;
using HomeChat.Backend.AIModels;
using HomeChat.Backend.Performances;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NReco.Logging.File;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(builder
    => builder.AddConsole()
    .AddFile("logs.log",
    configure: o =>
    {
        o.Append = true;
        o.MinLevel = LogLevel.Debug;
        o.HandleFileError = (err) =>
        {
            err.UseNewLogFileName($"{Path.GetFileNameWithoutExtension(err.LogFileName)}_alt{Path.GetExtension(err.LogFileName)}");
        };
    }
));
builder.Services.AddSingleton<IModelCollection, ModelCollection>();
builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();

app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

app.MapGet("/api/Prompt", HandlePrompt);

app.MapGet("/api/PerformanceSummary", async ([FromServices] IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

async Task<EmptyHttpResult> HandlePrompt(
    [FromQuery] string prompt,
    [FromQuery] int maxTokens,
    [FromServices] ILogger<Program> logger,
    [FromServices] ITextProcessor textProcessor,
    HttpContext context,
    CancellationToken cancellationToken)
{
    logger.LogInformation("Remote IP: {RemoteIpAdress} Prompt :{Prompt}", context.Connection.RemoteIpAddress, prompt);
    context.Response.Headers.Append("Content-Type", "text/event-stream");

    await textProcessor.Process(
        prompt,
        maxTokens,
        onNewText: async (text) =>
        {
            logger.LogInformation("{Text}", text);

            await context.Response.WriteAsync("event: aiMessage\n");
            await context.Response.WriteAsync("data: ");
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { newText = text }));
            await context.Response.WriteAsync($"\n\n");
            await context.Response.Body.FlushAsync();

        }, cancellationToken);
    await context.Response.WriteAsync("event: aiMessage\n");
    await context.Response.WriteAsync("data: \"{'newText':''}\"");
    await context.Response.WriteAsync($"\n\n");
    await context.Response.Body.FlushAsync();
    return TypedResults.Empty;
}

app.MapPost("/api/Reset", (ITextProcessor textProcessor) => textProcessor.LoadSelectedModel());

app.MapPost("/api/Models", async ([FromBody] ModelChange modelShortName, [FromServices] ITextProcessor textProcessor, [FromServices] IModelCollection models) =>
{
    models.SelectModel(modelShortName.NewModelShortName);
    await textProcessor.LoadSelectedModel();
});

app.MapGet("/api/Models", async ([FromServices] IModelCollection models) =>
{
    return await models.GetModels();
});

async Task HandleHome(HttpContext httpContext)
{
    var html = await File.ReadAllTextAsync(@"./wwwroot/index.html");
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    httpContext.Response.ContentType = MediaTypeNames.Text.Html;
    httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
    await httpContext.Response.WriteAsync(html);
}

await app.RunAsync();

public partial class Program { protected Program() { } }