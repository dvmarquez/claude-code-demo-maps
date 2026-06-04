using dotenv.net;
using MapsDemo.Api;
using MapsDemo.Api.Data;
using MapsDemo.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var envCandidates = new[]
{
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env")),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../.env")),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.env")),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.env")),
};

var envFile = envCandidates.FirstOrDefault(File.Exists);
if (envFile is not null)
    DotEnv.Load(options: new DotEnvOptions(envFilePaths: [envFile]));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var corsOrigin = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGIN") ?? "http://localhost:5173";
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var apiKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
if (string.IsNullOrEmpty(apiKey))
    throw new InvalidOperationException("GOOGLE_MAPS_API_KEY environment variable is not set.");
builder.Services.AddSingleton(new MapsApiSettings(apiKey));

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
