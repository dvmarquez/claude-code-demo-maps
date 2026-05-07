using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MapsDemo.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../.env"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../../.env"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../../../.env"),
        };

        var envFile = candidates.FirstOrDefault(File.Exists);
        if (envFile is not null)
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: [envFile]));

        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? $"Host=localhost;Port=5432;" +
               $"Database={Environment.GetEnvironmentVariable("POSTGRES_DB")};" +
               $"Username={Environment.GetEnvironmentVariable("POSTGRES_USER")};" +
               $"Password={Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")}";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
