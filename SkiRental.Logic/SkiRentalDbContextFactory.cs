using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SkiRental.Logic;

/// <summary>
/// Design-time factory for EF Core tools (migrations, update-database).
/// Required because the startup project is .NET MAUI, which EF tooling can't always bootstrap.
/// </summary>
public sealed class SkiRentalDbContextFactory : IDesignTimeDbContextFactory<SkiRentalDbContext>
{
    public SkiRentalDbContext CreateDbContext(string[] args)
    {
        // 1) Allow overriding via environment variable (CI / dev machines)
        var envConn = Environment.GetEnvironmentVariable("SKIRENTAL_CONNECTIONSTRING");
        if (!string.IsNullOrWhiteSpace(envConn))
            return Create(envConn);

        // 2) Try to load appsettings.json from the MAUI app project
        var appsettingsPath = FindUpwards("SkiRental.App", "appsettings.json")
                           ?? FindUpwards(null, "appsettings.json");

        if (appsettingsPath is not null)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(appsettingsPath)!)
                .AddJsonFile(Path.GetFileName(appsettingsPath), optional: false, reloadOnChange: false)
                .Build();

            var conn = config.GetConnectionString("SkiRentalSqlite")
                    ?? config.GetConnectionString("SkiRentalSqlServer");

            if (!string.IsNullOrWhiteSpace(conn))
                return Create(conn);
        }

        // 3) Safe fallback: local SQLite file in the working directory
        return Create("Data Source=SkiRental.DesignTime.db");
    }

    private static SkiRentalDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SkiRentalDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SkiRentalDbContext(options);
    }

    private static string? FindUpwards(string? projectDirName, string fileName)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        for (var i = 0; i < 8 && dir is not null; i++)
        {
            if (projectDirName is not null)
            {
                var candidate = Path.Combine(dir.FullName, projectDirName, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            {
                var candidate = Path.Combine(dir.FullName, fileName);
                if (File.Exists(candidate))
                    return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

