using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace PokenaeTemplate.Infrastructure.Data;

internal sealed partial class DesignTimeConnectionStringResolver
{
    private readonly string _baseDirectory;
    private readonly string _currentDirectory;

    public DesignTimeConnectionStringResolver()
        : this(Directory.GetCurrentDirectory(), AppContext.BaseDirectory)
    {
    }

    internal DesignTimeConnectionStringResolver(string currentDirectory, string baseDirectory)
    {
        _currentDirectory = currentDirectory;
        _baseDirectory = baseDirectory;
    }

    public string Resolve()
    {
        var composeSettings = ComposeSettings.Load(EnumerateSearchRoots());
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? FindConfiguredConnectionString()
            ?? BuildFallbackConnectionString(composeSettings);

        return NormalizeConnectionString(ExpandPlaceholders(connectionString, composeSettings), composeSettings);
    }

    private string? FindConfiguredConnectionString()
    {
        foreach (var configDirectory in EnumerateCandidateConfigDirectories())
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                environmentName = "Development";
            }

            string? connectionString = null;
            foreach (var fileName in new[] { "appsettings.json", $"appsettings.{environmentName}.json" })
            {
                var filePath = Path.Combine(configDirectory, fileName);
                var candidate = ReadConnectionStringFromFile(filePath);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    connectionString = candidate;
                }
            }

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        return null;
    }

    private IEnumerable<string> EnumerateCandidateConfigDirectories()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateSearchRoots())
        {
            if (seen.Add(root))
            {
                yield return root;
            }

            foreach (var webDirectory in EnumerateWebDirectories(root))
            {
                if (seen.Add(webDirectory))
                {
                    yield return webDirectory;
                }
            }
        }
    }

    private IEnumerable<string> EnumerateSearchRoots()
    {
        foreach (var path in EnumerateAncestors(_currentDirectory, 6))
        {
            yield return path;
        }

        foreach (var path in EnumerateAncestors(_baseDirectory, 8))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateAncestors(string startPath, int maxDepth)
    {
        var current = new DirectoryInfo(startPath);
        for (var depth = 0; current is not null && depth <= maxDepth; depth++, current = current.Parent)
        {
            yield return current.FullName;
        }
    }

    private static IEnumerable<string> EnumerateWebDirectories(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            yield break;
        }

        IEnumerator<string>? directories = null;
        try
        {
            directories = Directory.EnumerateDirectories(rootPath, "*.Web", SearchOption.TopDirectoryOnly).GetEnumerator();
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        using (directories)
        {
            while (directories.MoveNext())
            {
                yield return directories.Current;
            }
        }
    }

    private static string? ReadConnectionStringFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStringsElement))
        {
            return null;
        }

        if (!connectionStringsElement.TryGetProperty("DefaultConnection", out var defaultConnectionElement))
        {
            return null;
        }

        return defaultConnectionElement.GetString();
    }

    private static string ExpandPlaceholders(string value, ComposeSettings composeSettings)
    {
        return EnvironmentPlaceholderRegex().Replace(value, match =>
        {
            var name = match.Groups["name"].Value;
            var fallback = match.Groups["fallback"].Success ? match.Groups["fallback"].Value : string.Empty;
            var resolved = Environment.GetEnvironmentVariable(name) ?? composeSettings.Get(name);
            return string.IsNullOrWhiteSpace(resolved) ? fallback : resolved;
        });
    }

    private static string NormalizeConnectionString(string connectionString, ComposeSettings composeSettings)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!IsRunningInContainer() && IsComposeDatabaseHost(builder.DataSource))
        {
            builder.DataSource = $"127.0.0.1,{composeSettings.DbHostPort}";
            builder.Password = composeSettings.Password;
        }
        else if (string.IsNullOrWhiteSpace(builder.Password) || string.Equals(builder.Password, "YourPassword123!", StringComparison.Ordinal))
        {
            builder.Password = composeSettings.Password;
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            builder.InitialCatalog = composeSettings.Database;
        }

        return builder.ConnectionString;
    }

    private static string BuildFallbackConnectionString(ComposeSettings composeSettings)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = IsRunningInContainer() ? "db,1433" : $"127.0.0.1,{composeSettings.DbHostPort}",
            InitialCatalog = composeSettings.Database,
            UserID = "sa",
            Password = composeSettings.Password,
            TrustServerCertificate = true,
        };

        return builder.ConnectionString;
    }

    private static bool IsComposeDatabaseHost(string? dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return false;
        }

        var host = dataSource.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        return string.Equals(host, "db", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "mssql", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRunningInContainer()
    {
        return string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ComposeSettings
    {
        private readonly Dictionary<string, string> _values;

        private ComposeSettings(Dictionary<string, string> values)
        {
            _values = values;
        }

        public string Password => Get("MSSQL_SA_PASSWORD") ?? "Sql1Password";

        public string Database => Get("MSSQL_DB") ?? "PokenaeTemplate";

        public string DbHostPort => Get("DB_HOST_PORT") ?? "18033";

        public string? Get(string key)
        {
            if (_values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var environmentValue = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(environmentValue) ? null : environmentValue;
        }

        public static ComposeSettings Load(IEnumerable<string> searchRoots)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var filePath in EnumerateComposeEnvFiles(searchRoots))
            {
                foreach (var entry in ParseEnvFile(filePath))
                {
                    values[entry.Key] = entry.Value;
                }

                if (values.Count > 0)
                {
                    break;
                }
            }

            return new ComposeSettings(values);
        }

        private static IEnumerable<string> EnumerateComposeEnvFiles(IEnumerable<string> searchRoots)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in searchRoots)
            {
                foreach (var fileName in new[] { ".env.docker.debug", ".env.docker.development", ".env.docker.production" })
                {
                    var filePath = Path.Combine(root, fileName);
                    if (File.Exists(filePath) && seen.Add(filePath))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        private static Dictionary<string, string> ParseEnvFile(string filePath)
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
    }

    [GeneratedRegex(@"\$\{(?<name>[A-Za-z0-9_]+)(:-(?<fallback>[^}]*))?\}")]
    private static partial Regex EnvironmentPlaceholderRegex();
}