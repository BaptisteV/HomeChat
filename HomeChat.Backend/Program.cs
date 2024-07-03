using HomeChat.Backend;
using HomeChat.Backend.AIModels;
using HomeChat.Backend.Chats;
using HomeChat.Backend.Performances;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using NReco.Logging.File;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields =
    HttpLoggingFields.RequestPath
    | HttpLoggingFields.RequestQuery
    | HttpLoggingFields.RequestProtocol
    | HttpLoggingFields.ResponseBody
    | HttpLoggingFields.ResponseStatusCode;

    o.MediaTypeOptions.AddText("application/json");
    o.RequestBodyLogLimit = 4096;
    o.ResponseBodyLogLimit = 4096;
});

builder.Services.AddProblemDetails();

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
                o.HandleFileError = (err) => err.UseNewLogFileName($"{Path.GetFileNameWithoutExtension(err.LogFileName)}_alt{Random.Shared.Next()}{Path.GetExtension(err.LogFileName)}");
            }
        )
);

builder.Services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
builder.Services.AddHostedService<InactiveSessionCleanerService>();
builder.Services.AddSingleton<IChat, Chat>();
builder.Services.AddTransient<IModelCollection, ModelCollection>();
builder.Services.AddSingleton<ISessionCleanerService, SessionCleanerService>();
builder.Services.AddSingleton<IChatSessionManager, ChatSessionManager>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChatResponseWriter, ChatResponseWriter>();
builder.Services.AddScoped<IPerformanceSummaryWriter, PerformanceSummaryWriter>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseHttpLogging();
app.UseExceptionHandler();
app.UseDeveloperExceptionPage();

app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

async Task HandleHome(HttpContext httpContext)
{
    httpContext.Response.ContentType = "text/html";
    var html = await File.ReadAllTextAsync(@"./wwwroot/index.html");
    await httpContext.Response.WriteAsync(html);
}

app.MapGet("/api/PerformanceSummary", PerformanceSummary);
async Task<EmptyHttpResult> PerformanceSummary(
    [FromServices] IPerformanceMonitor performanceMonitor,
    [FromServices] IPerformanceSummaryWriter summaryWriter,
    [FromQuery] int interval,
    HttpContext context,
    CancellationToken cancellationToken,
    [FromQuery] int? stopAfter = null)
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    var reportCount = 0;
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(interval));
    while (await timer.WaitForNextTickAsync() && !cancellationToken.IsCancellationRequested)
    {
        if (stopAfter is not null && reportCount >= stopAfter)
            break;

        var perf = performanceMonitor.GetPerformanceSummary();
        await summaryWriter.Write(perf);

        reportCount++;
    }

    return TypedResults.Empty;
}

app.MapChatEndpoints();
app.MapModelsEndpoints();

await app.RunAsync();

public partial class Program { protected Program() { } }