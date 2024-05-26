using HomeChat.Client.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<HomeChatClient>();
builder.Services.AddSpeechSynthesis();
builder.Services.AddHttpClient();

var app = builder.Build();
await app.RunAsync();