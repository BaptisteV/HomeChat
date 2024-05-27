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
builder.Services.AddSingleton<IModelCollection>(new ModelCollection());
builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();

app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

app.MapGet("/prompt", HandlePrompt);

app.MapGet("PerformanceSummary", async (IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

async Task<EmptyHttpResult> HandlePrompt(
    ITextProcessor textProcessor,
    HttpContext context,
    CancellationToken cancellationToken,
    [FromQuery] string prompt,
    [FromQuery] int maxTokens = 50)
{
    //Console.WriteLine("/prompt Handling...");
    context.Response.Headers.Append("Content-Type", "text/event-stream");

    await textProcessor.Process(
        prompt,
        maxTokens,
        onNewText: async (text) =>
        {
            Console.Write(text);

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

app.MapPost("/reset", (ITextProcessor textProcessor) => textProcessor.LoadSelectedModel());

app.MapGet("/models", (IModelCollection models) =>
{
    Console.WriteLine("Retrieving models...");
    return models.GetModels();
});

app.MapPost("/models", async (ITextProcessor textProcessor, [FromBody] ModelChange modelShortName, IModelCollection models) =>
{
    models.SelectModel(modelShortName.NewModelShortName);
    textProcessor.LoadSelectedModel();
});

async Task HandleHome(HttpContext httpContext)
{
    var html = await File.ReadAllTextAsync(@"./wwwroot/index.html");
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    httpContext.Response.ContentType = MediaTypeNames.Text.Html;
    httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
    await httpContext.Response.WriteAsync(html);
}

app.Run();

public partial class Program { protected Program() { } }