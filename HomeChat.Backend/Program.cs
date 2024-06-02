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

builder.Services.AddSingleton<IModelCollection, ModelCollection>();
builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChatResponseWriter, ChatResponseWriter>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseHttpLogging();


app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

app.MapGet("/api/Prompt", HandlePrompt);

app.MapGet("/api/PerformanceSummary", async ([FromServices] IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

app.MapPost("/api/Models", async ([FromBody] ModelChange modelShortName, [FromServices] ITextProcessor textProcessor, [FromServices] IModelCollection models) =>
{
    models.SelectModel(modelShortName.NewModelShortName);
    await textProcessor.LoadSelectedModel();
});

app.MapGet("/api/Models", async ([FromServices] IModelCollection models) =>
{
    return await models.GetModels();
});

async Task<EmptyHttpResult> HandlePrompt(
    [FromQuery] string prompt,
    [FromQuery] int maxTokens,
    [FromServices] ILogger<Program> logger,
    [FromServices] ITextProcessor textProcessor,
    [FromServices] IChatResponseWriter chatResponseWriter,
    HttpContext context,
    CancellationToken cancellationToken)
{
    logger.LogInformation("Remote IP: {RemoteIpAdress} Prompt :{Prompt}", context.Connection.RemoteIpAddress, prompt);
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    var wholeResponse = "";
    await textProcessor.Process(
        prompt,
        maxTokens,
        onNewText: async (text) =>
        {
            if (string.IsNullOrEmpty(text))
                return;
            Console.Write(text);
            wholeResponse += text;
            await chatResponseWriter.WriteEvent(text);
            /*await context.Response.WriteAsync("event: aiMessage\n");
            await context.Response.WriteAsync("data: ");
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { newText = text }));
            await context.Response.WriteAsync($"\n\n");
            await context.Response.Body.FlushAsync();*/

        }, cancellationToken);
    logger.LogInformation("Ai response: {Response}", wholeResponse);
    await context.Response.WriteAsync("event: aiMessage\n");
    await context.Response.WriteAsync("data: \"{'newText':''}\"");
    await context.Response.WriteAsync($"\n\n");
    await context.Response.Body.FlushAsync();

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