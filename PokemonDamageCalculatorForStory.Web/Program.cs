using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

builder.Services.Configure<GoogleAuthenticationOptions>(
    builder.Configuration.GetSection(GoogleAuthenticationOptions.SectionName));

builder.Services.AddHttpClient<IGoogleAccessTokenValidationService, GoogleAccessTokenValidationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PokemonDamageCalculatorForStory API",
            Version = "v1",
            Description = "Authorization ヘッダーに Google access token を指定して、保護された Web API を試せます。"
        });

        options.AddSecurityDefinition(GoogleAuthenticationDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "Google access token",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Google ログインで取得した access token を貼り付けてください。JWT 発行エンドポイントは不要です。"
        });

        options.OperationFilter<AuthorizeOperationFilter>();
    });

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = GoogleAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleAuthenticationDefaults.AuthenticationScheme;
    })
    .AddScheme<AuthenticationSchemeOptions, GoogleAccessTokenAuthenticationHandler>(
        GoogleAuthenticationDefaults.AuthenticationScheme,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.ManageWeatherForecast, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new WeatherForecastManagementRequirement());
    });
});

// MediatR 登録（Application層のHandlers自動登録）
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(PokemonDamageCalculatorForStory.Application.UseCases.Commands.CreateWeatherForecastCommand).Assembly));

// Repository 登録
builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
builder.Services.AddScoped<IUserAuthorizationInfoRepository, UserAuthorizationInfoRepository>();
builder.Services.AddScoped<IWeatherForecastAccessEvaluator, WeatherForecastAccessEvaluator>();
builder.Services.AddScoped<IAuthorizationHandler, WeatherForecastManagementAuthorizationHandler>();

// FluentValidation 登録（将来:バリデーション Pipeline を追加予定）
// builder.Services.AddValidatorsFromAssemblyContaining<CreateWeatherForecastCommandValidator>();

var app = builder.Build();
var useHttpsRedirection = shouldUseHttpsRedirection(app.Configuration);

if (!app.Environment.IsProduction())
{
    await applyDatabaseMigrationsAsync(app);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static bool shouldUseHttpsRedirection(IConfiguration configuration)
{
    var isRunningInContainer = string.Equals(
        configuration["DOTNET_RUNNING_IN_CONTAINER"],
        "true",
        StringComparison.OrdinalIgnoreCase);

    if (!isRunningInContainer)
    {
        return true;
    }

    return !string.IsNullOrWhiteSpace(configuration["ASPNETCORE_HTTPS_PORTS"])
        || !string.IsNullOrWhiteSpace(configuration["HTTPS_PORT"])
        || (!string.IsNullOrWhiteSpace(configuration["ASPNETCORE_URLS"])
            && configuration["ASPNETCORE_URLS"]!.Contains("https://", StringComparison.OrdinalIgnoreCase));
}

static async Task applyDatabaseMigrationsAsync(WebApplication app)
{
    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(5);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            if (!db.Database.IsRelational())
            {
                return;
            }

            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                app.Logger.LogInformation("Migrationが作成されていません. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
            }

            return;
        }
        catch (SqlException ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "SQL Server is not ready yet. Retrying database migration. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }
        catch (InvalidOperationException ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "Database migration failed during startup. Retrying. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }

        await Task.Delay(delay);
    }
}

public partial class Program;
