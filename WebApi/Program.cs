using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

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

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("FrontendCors");

app.UseAuthorization();

app.MapControllers();

app.Run();