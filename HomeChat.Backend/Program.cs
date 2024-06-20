using HomeChat.Backend;
using HomeChat.Backend.AIModels;
using HomeChat.Backend.Chats;
using HomeChat.Backend.Performances;
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
builder.Services.AddSingleton<IChat, Chat>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IChatResponseWriter, ChatResponseWriter>();

var app = builder.Build();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseHttpLogging();
app.UseExceptionHandler();

app.MapGet("/", HandleHome);
app.MapGet("/Home", HandleHome);

async Task HandleHome(HttpContext httpContext)
{
    httpContext.Response.ContentType = "text/html";
    var html = await File.ReadAllTextAsync(@"./wwwroot/index.html");
    await httpContext.Response.WriteAsync(html);
}

app.MapGet("/api/PerformanceSummary", async ([FromServices] IPerformanceMonitor performanceMonitor)
    => await performanceMonitor.GetPerformanceSummaryAsync());

app.MapChatEndpoints();
app.MapModelsEndpoints();

await app.RunAsync();

public partial class Program { protected Program() { } }