using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Infrastructure.Data;

namespace PokemonDamageCalculatorForStory.Tests.Infrastructure.TestSupport;

internal sealed class InfrastructureSqlServerTestDatabase : IAsyncDisposable
{
    private readonly string _connectionString;

    private InfrastructureSqlServerTestDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static async Task<InfrastructureSqlServerTestDatabase> CreateAsync()
    {
        var settings = TestDatabaseSettings.Load();
        var databaseName = $"{settings.DatabasePrefix}_InfrastructureTests_{Guid.NewGuid():N}";
        var connectionString = $"Server=127.0.0.1,{settings.HostPort};Database={databaseName};User Id=sa;Password={settings.Password};TrustServerCertificate=True;Pooling=False;";

        var database = new InfrastructureSqlServerTestDatabase(connectionString);
        await database.InitializeAsync();
        return database;
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure())
            .Options;

        return new AppDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await using var context = CreateDbContext();
                await context.Database.EnsureDeletedAsync();
            });
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task InitializeAsync()
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await using var context = CreateDbContext();
            await context.Database.MigrateAsync();
        });
    }

    private static async Task ExecuteWithRetryAsync(Func<Task> action)
    {
        const int maxAttempts = 12;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception) when (attempt < maxAttempts)
            {
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("SQL Server test database is not available. Start the Docker Compose task 'Docker Compose: Dotnet Test DB Up' before running Infrastructure SQL tests.");
    }

    private sealed class TestDatabaseSettings
    {
        private readonly IReadOnlyDictionary<string, string> _values;

        private TestDatabaseSettings(IReadOnlyDictionary<string, string> values)
        {
            _values = values;
        }

        public string DatabasePrefix => Get("MSSQL_DB") ?? "PokemonDamageCalculatorForStory";

        public string HostPort => Get("TEST_DB_HOST_PORT") ?? "20034";

        public string Password => Get("MSSQL_SA_PASSWORD") ?? "Sql1Password";

        public static TestDatabaseSettings Load()
        {
            return new TestDatabaseSettings(LoadValues());
        }

        private static IEnumerable<string> EnumerateAncestors(string startPath, int maxDepth)
        {
            var current = new DirectoryInfo(startPath);
            for (var depth = 0; current is not null && depth <= maxDepth; depth++, current = current.Parent)
            {
                yield return current.FullName;
            }
        }

        private static IReadOnlyDictionary<string, string> LoadValues()
        {
            var envFile = FindEnvFile();
            return envFile is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : ParseEnvFile(envFile);
        }

        private static string? FindEnvFile()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in EnumerateAncestors(Directory.GetCurrentDirectory(), 6))
            {
                if (seen.Add(root))
                {
                    var candidate = Path.Combine(root, ".env.docker.test");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            foreach (var root in EnumerateAncestors(AppContext.BaseDirectory, 8))
            {
                if (seen.Add(root))
                {
                    var candidate = Path.Combine(root, ".env.docker.test");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static IReadOnlyDictionary<string, string> ParseEnvFile(string filePath)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                values[key] = value;
            }

            return values;
        }

        private string? Get(string key)
        {
            var environmentValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }

            return _values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }
    }
}
