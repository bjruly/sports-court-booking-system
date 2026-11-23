using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SportsCourtBooking.Desktop.Core;

/// <summary>
/// Chuỗi kết nối PostgreSQL, lấy theo thứ tự:
/// 1. biến môi trường hoặc appsettings.json (ConnectionStrings:DefaultConnection);
/// 2. file .env của repository (cùng file mà docker compose dùng), kết nối tới localhost:5432.
/// </summary>
public static class ConnectionSettings
{
    public static string? Resolve(string baseDirectory)
    {
        var configured = new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("DefaultConnection");

        return string.IsNullOrWhiteSpace(configured) ? FromEnvFile(baseDirectory) : configured;
    }

    private static string? FromEnvFile(string baseDirectory)
    {
        for (var directory = new DirectoryInfo(baseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, ".env");
            if (File.Exists(path) && File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            {
                return Parse(File.ReadAllLines(path));
            }
        }

        return null;
    }

    private static string? Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>();
        foreach (var line in lines.Select(item => item.Trim()).Where(item => !item.StartsWith('#')))
        {
            var separator = line.IndexOf('=');
            if (separator > 0)
            {
                values[line[..separator].Trim()] = line[(separator + 1)..].Trim().Trim('"', '\'');
            }
        }

        if (!values.TryGetValue("POSTGRES_PASSWORD", out var password) || password.Length == 0)
        {
            return null;
        }

        return new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = values.GetValueOrDefault("POSTGRES_DB", "sportscourt"),
            Username = values.GetValueOrDefault("POSTGRES_USER", "postgres"),
            Password = password
        }.ConnectionString;
    }
}
