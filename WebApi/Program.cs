using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using DotNetEnv;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Wczytaj .env (jeśli istnieje) dla lokalnego uruchomienia poza Dockerem
// W Dockerze zmienne środowiskowe będą wstrzyknięte przez orchestrator
try
{
    // Szukaj .env w bieżącym i nadrzędnych katalogach (root repo)
    Env.TraversePath().Load();
}
catch
{
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for Frontend (Blazor WASM)
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        var allowedOrigins = new[]
        {
            builder.Configuration["Frontend:Url"] ?? "http://127.0.0.1:8888",
            "http://127.0.0.1:8888"
        };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Forwarded headers (X-Forwarded-For / X-Forwarded-Proto) dla pracy za reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Konfiguracja HttpClientFactory dla Spotify API
builder.Services.AddHttpClient("SpotifyApi", client =>
{
    var spotifyApiUrl = builder.Configuration["Spotify:ApiUrl"] ?? "https://api.spotify.com/v1/";
    client.BaseAddress = new Uri(spotifyApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "SpotifyBasicApi2025/1.0");
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<TokenManagerService>();

// Serwisy Spotify API
builder.Services.AddScoped<SpotifyRequestService>();
builder.Services.AddScoped<SpotifyApiService>();
builder.Services.AddScoped<PlayerService>();

// Serwisy Library
builder.Services.AddScoped<TagsService>();
// Serwisy User
builder.Services.AddScoped<UserService>();
// Serwis Library
builder.Services.AddScoped<LibraryService>();

builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var provider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? builder.Configuration["Database:Provider"] ?? "SqlServer";

    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DbConnectionString")
        ?? builder.Configuration.GetConnectionString("DbConnectionString")
        ?? string.Empty;

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Brak connection stringa. Ustaw ConnectionStrings__DbConnectionString w env lub appsettings.");

    if (string.Equals(provider, "MySql", StringComparison.OrdinalIgnoreCase))
    {
        var mySqlVersion = new MySqlServerVersion(new Version(8, 0, 36));
        options.UseMySql(connectionString, mySqlVersion);
        return;
    }

    options.UseSqlServer(connectionString);
});

// Konfiguracja portu dla API (backend) - oddzielnie od frontu
builder.WebHost.UseUrls("http://*:8889");

var app = builder.Build();

// Auto-migracje bazy danych przy starcie aplikacji
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

if (!isContainer)
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseForwardedHeaders();

app.UseCors("FrontendCors");

app.UseAuthorization();

app.MapControllers();

app.Run();