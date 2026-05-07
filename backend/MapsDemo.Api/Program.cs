using MapsDemo.Api.Data;
using MapsDemo.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

// Load .env by searching up the directory tree from known locations
var envCandidates = new[]
{
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../.env")),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.env")),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.env")),
};

var envFile = envCandidates.FirstOrDefault(File.Exists);
if (envFile is not null)
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var idx = line.IndexOf('=');
        if (idx < 0) continue;
        Environment.SetEnvironmentVariable(line[..idx].Trim(), line[(idx + 1)..].Trim());
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
    ?? $"Host=localhost;Port=5432;" +
       $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB")};" +
       $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")};" +
       $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")}";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient("GoogleMaps");

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.MapMapsEndpoints();

app.Run();
