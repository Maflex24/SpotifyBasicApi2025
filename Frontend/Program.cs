using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Użyj adresu API z konfiguracji (wwwroot/appsettings.json → "ApiBase")
var apiBase = builder.Configuration["ApiBase"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(sp =>
{
    var handler = new IncludeCredentialsHandler { InnerHandler = new HttpClientHandler() };
    return new HttpClient(handler) { BaseAddress = new Uri(apiBase) };
});

// Konfiguracja Bootstrap w dark theme
builder.Services.AddSingleton(new FrontendTheme
{
    BackgroundHex = "#0b0b0f",
    ForegroundHex = "#e5e7eb"
});

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AiChatDrawerService>();
builder.Services.AddScoped<BlindModeService>();
builder.Services.AddScoped<ActivePlaybackService>();
builder.Services.AddScoped<PlaybackService>();
builder.Services.AddScoped<TagClipboardService>();

await builder.Build().RunAsync();
