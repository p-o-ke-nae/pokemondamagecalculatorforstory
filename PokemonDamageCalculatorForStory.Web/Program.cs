using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.Behaviors;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;
using PokemonDamageCalculatorForStory.Application.Validators;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;
using DomainValidationException = PokemonDamageCalculatorForStory.Domain.Exceptions.ValidationException;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

builder.Services.Configure<GoogleAuthenticationOptions>(builder.Configuration.GetSection(GoogleAuthenticationOptions.SectionName));

builder.Services.AddHttpClient<IGoogleAccessTokenValidationService, GoogleAccessTokenValidationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PokemonDamageCalculatorForStory API",
        Version = "v1",
        Description = "ストーリー攻略用ポケモンダメージ計算 Web API"
    });

    options.AddSecurityDefinition(GoogleAuthenticationDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Google access token",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Google ログインで取得した access token を貼り付けてください。"
    });

    options.OperationFilter<AuthorizeOperationFilter>();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = GoogleAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleAuthenticationDefaults.AuthenticationScheme;
})
.AddScheme<AuthenticationSchemeOptions, GoogleAccessTokenAuthenticationHandler>(GoogleAuthenticationDefaults.AuthenticationScheme, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateRunCommand).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(CreateRunCommandValidator).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<IRuleSetRepository, RuleSetRepository>();
builder.Services.AddScoped<IRunRepository, RunRepository>();
builder.Services.AddScoped<IBattleRepository, BattleRepository>();
builder.Services.AddScoped<IUserAuthorizationInfoRepository, UserAuthorizationInfoRepository>();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    await applyDatabaseMigrationsAsync(app);
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var problemDetails = exception switch
        {
            NotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = notFoundException.Message
            },
            DomainValidationException validationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed.",
                Detail = validationException.Message
            },
            DomainException domainException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Domain rule violated.",
                Detail = domainException.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (shouldUseHttpsRedirection(app.Configuration))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

static bool shouldUseHttpsRedirection(IConfiguration configuration)
{
    var isRunningInContainer = string.Equals(configuration["DOTNET_RUNNING_IN_CONTAINER"], "true", StringComparison.OrdinalIgnoreCase);

    if (!isRunningInContainer)
    {
        return true;
    }

    return !string.IsNullOrWhiteSpace(configuration["ASPNETCORE_HTTPS_PORTS"])
        || !string.IsNullOrWhiteSpace(configuration["HTTPS_PORT"])
        || (!string.IsNullOrWhiteSpace(configuration["ASPNETCORE_URLS"]) && configuration["ASPNETCORE_URLS"]!.Contains("https://", StringComparison.OrdinalIgnoreCase));
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
            app.Logger.LogWarning(ex, "SQL Server is not ready yet. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }
        catch (InvalidOperationException ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "Database migration failed. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }

        await Task.Delay(delay);
    }
}

public partial class Program;
