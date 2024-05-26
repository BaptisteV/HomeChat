using HomeChat.Backend;
using HomeChat.Backend.AIModels;
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

/*builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyMethod()
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .SetIsOriginAllowed(origin => true)
                .WithMethods("GET, PATCH, DELETE, PUT, POST, OPTIONS"));
});*/
var app = builder.Build();
//app.UseCors("CorsPolicy");

app.UseStaticFiles();
app.UseHttpsRedirection();
var models = await app.Services.GetRequiredService<IModelCollection>().GetModels();
var defaultModel = models.Single(m => m.Selected);
var staticTextProcessor = new TextProcessor(defaultModel.Filename);

app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

app.MapGet("/prompt", HandlePrompt);

app.MapGet("PerformanceSummary", async (IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

async Task<EmptyHttpResult> HandlePrompt(
    HttpContext context,
    CancellationToken cancellationToken,
    [FromQuery] string prompt,
    [FromQuery] int maxTokens = 50)
{
    //Console.WriteLine("/prompt Handling...");
    context.Response.Headers.Append("Content-Type", "text/event-stream");

    if (!staticTextProcessor.Started)
        staticTextProcessor.Start();

    await staticTextProcessor.Process(
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

app.MapPost("/reset", () => staticTextProcessor.Start());

app.MapGet("/models", (IModelCollection models) =>
{
    Console.WriteLine("Retrieving models...");
    return models.GetModels();
});

app.MapPost("/models", async ([FromBody] ModelChange modelShortName, IModelCollection models) =>
{
    var newModel = models.SelectModel(modelShortName.NewModelShortName);
    await staticTextProcessor.DisposeAsync();
    staticTextProcessor = new TextProcessor(newModel.Filename);
    staticTextProcessor.Start();
});

async Task HandleHome(HttpContext httpContext)
{
    var html = await File.ReadAllTextAsync(@"C:\Users\Bapt\source\repos\HomeChat.Backend\wwwroot\index.html");
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    httpContext.Response.ContentType = MediaTypeNames.Text.Html;
    httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
    await httpContext.Response.WriteAsync(html);
}

app.Run();

public partial class Program { protected Program() { } }