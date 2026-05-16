using System.Text.Json;
using Microsoft.Data.SqlClient;
using PokenaeTemplate.Infrastructure.Data;
using Xunit;

namespace PokenaeTemplate.Tests.Infrastructure.Data;

[Collection("DesignTimeConnectionStringResolver")]
public sealed class DesignTimeConnectionStringResolverTests
{
    [Fact]
    public void Resolve_Prefers_ConnectionString_EnvironmentVariable()
    {
        using var scope = new ResolverTestScope();
        scope.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        scope.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        scope.SetEnvironmentVariable("MSSQL_SA_PASSWORD", null);
        scope.SetEnvironmentVariable("MSSQL_DB", null);
        scope.SetEnvironmentVariable("DB_HOST_PORT", null);
        scope.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=127.0.0.1,15433;Database=EnvDb;User Id=sa;Password=EnvPassword1!;TrustServerCertificate=True;");
        scope.WriteWebAppSettings(baseConnectionString: "Server=db,1433;Database=ConfigDb;User Id=sa;Password=ConfigPassword1!;TrustServerCertificate=True;");

        var connectionString = scope.CreateResolver().Resolve();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("127.0.0.1,15433", builder.DataSource);
        Assert.Equal("EnvDb", builder.InitialCatalog);
        Assert.Equal("EnvPassword1!", builder.Password);
    }

    [Fact]
    public void Resolve_Uses_EnvironmentSpecific_AppSettings_And_Normalizes_Compose_Host()
    {
        using var scope = new ResolverTestScope();
        scope.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        scope.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        scope.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        scope.SetEnvironmentVariable("MSSQL_SA_PASSWORD", null);
        scope.SetEnvironmentVariable("MSSQL_DB", null);
        scope.SetEnvironmentVariable("DB_HOST_PORT", null);
        scope.WriteWebAppSettings(
            baseConnectionString: "Server=127.0.0.1,14033;Database=BaseDb;User Id=sa;Password=BasePassword1!;TrustServerCertificate=True;",
            environmentConnectionString: "Server=db,1433;Database=;User Id=sa;Password=${MSSQL_SA_PASSWORD:-Sql1Password};TrustServerCertificate=True;");
        scope.WriteComposeSettings(
            ("MSSQL_SA_PASSWORD", "ComposePassword1!"),
            ("MSSQL_DB", "EnvironmentDb"),
            ("DB_HOST_PORT", "19033"));

        var connectionString = scope.CreateResolver().Resolve();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("127.0.0.1,19033", builder.DataSource);
        Assert.Equal("EnvironmentDb", builder.InitialCatalog);
        Assert.Equal("ComposePassword1!", builder.Password);
    }

    [Fact]
    public void Resolve_FallsBack_To_Compose_Settings_When_No_Configured_ConnectionString_Exists()
    {
        using var scope = new ResolverTestScope();
        scope.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        scope.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        scope.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        scope.SetEnvironmentVariable("MSSQL_SA_PASSWORD", null);
        scope.SetEnvironmentVariable("MSSQL_DB", null);
        scope.SetEnvironmentVariable("DB_HOST_PORT", null);
        scope.WriteComposeSettings(
            ("MSSQL_SA_PASSWORD", "FallbackPassword1!"),
            ("MSSQL_DB", "FallbackDb"),
            ("DB_HOST_PORT", "19133"));

        var connectionString = scope.CreateResolver().Resolve();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("127.0.0.1,19133", builder.DataSource);
        Assert.Equal("FallbackDb", builder.InitialCatalog);
        Assert.Equal("FallbackPassword1!", builder.Password);
        Assert.Equal("sa", builder.UserID);
    }

    [Fact]
    public void Resolve_Normalizes_Legacy_Mssql_Host_And_Placeholder_Password_On_Host()
    {
        using var scope = new ResolverTestScope();
        scope.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        scope.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        scope.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        scope.SetEnvironmentVariable("MSSQL_SA_PASSWORD", null);
        scope.SetEnvironmentVariable("MSSQL_DB", null);
        scope.SetEnvironmentVariable("DB_HOST_PORT", null);
        scope.WriteWebAppSettings(
            environmentConnectionString: "Server=mssql;Database=;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;");
        scope.WriteComposeSettings(
            ("MSSQL_SA_PASSWORD", "LegacyComposePassword1!"),
            ("MSSQL_DB", "LegacyComposeDb"),
            ("DB_HOST_PORT", "19233"));

        var connectionString = scope.CreateResolver().Resolve();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("127.0.0.1,19233", builder.DataSource);
        Assert.Equal("LegacyComposeDb", builder.InitialCatalog);
        Assert.Equal("LegacyComposePassword1!", builder.Password);
    }

    private sealed class ResolverTestScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalEnvironmentVariables = new(StringComparer.OrdinalIgnoreCase);

        public ResolverTestScope()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "PokenaeTemplateResolverTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public DesignTimeConnectionStringResolver CreateResolver()
        {
            return new DesignTimeConnectionStringResolver(RootPath, RootPath);
        }

        public void WriteWebAppSettings(string? baseConnectionString = null, string? environmentConnectionString = null, string environmentName = "Development")
        {
            var webDirectory = Path.Combine(RootPath, "Sample.Web");
            Directory.CreateDirectory(webDirectory);

            if (!string.IsNullOrWhiteSpace(baseConnectionString))
            {
                File.WriteAllText(
                    Path.Combine(webDirectory, "appsettings.json"),
                    SerializeAppSettings(baseConnectionString));
            }

            if (!string.IsNullOrWhiteSpace(environmentConnectionString))
            {
                File.WriteAllText(
                    Path.Combine(webDirectory, $"appsettings.{environmentName}.json"),
                    SerializeAppSettings(environmentConnectionString));
            }
        }

        public void WriteComposeSettings(params (string Key, string Value)[] values)
        {
            var lines = values.Select(entry => $"{entry.Key}={entry.Value}");
            File.WriteAllLines(Path.Combine(RootPath, ".env.docker.debug"), lines);
        }

        public void SetEnvironmentVariable(string key, string? value)
        {
            if (!_originalEnvironmentVariables.ContainsKey(key))
            {
                _originalEnvironmentVariables[key] = Environment.GetEnvironmentVariable(key);
            }

            Environment.SetEnvironmentVariable(key, value);
        }

        public void Dispose()
        {
            foreach (var entry in _originalEnvironmentVariables)
            {
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }

            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        private static string SerializeAppSettings(string connectionString)
        {
            return JsonSerializer.Serialize(new
            {
                ConnectionStrings = new
                {
                    DefaultConnection = connectionString,
                },
            });
        }
    }
}

[CollectionDefinition("DesignTimeConnectionStringResolver", DisableParallelization = true)]
public sealed class DesignTimeConnectionStringResolverCollection
{
}