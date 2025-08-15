using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities.Context;
using SpotifyBasicApi2025.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Serwisy API
builder.Services.AddScoped<SpotifyDataService>();

builder.Services.AddScoped<SpotifyRequestService>();
builder.Services.AddScoped<SpotifyApiService>();
builder.Services.AddScoped<SpotifyMyProfileService>();

builder.Services.AddDbContext<DatabaseContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

// Konfiguracja portu 8888 dla lokalnego rozwoju - nasłuchiwanie na wszystkich adresach IP
builder.WebHost.UseUrls("http://*:8888");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();