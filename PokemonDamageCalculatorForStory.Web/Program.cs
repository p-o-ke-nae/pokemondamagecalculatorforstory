using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.Identity;
using PokemonDamageCalculatorForStory.Application.Services;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;
using PokemonDamageCalculatorForStory.Infrastructure.Seed;
using PokemonDamageCalculatorForStory.Web.Api;
using PokemonDamageCalculatorForStory.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var databaseProvider = Environment.GetEnvironmentVariable("POKEMON_STORY_DATABASE_PROVIDER")
    ?? builder.Configuration["Database:Provider"];
if (string.Equals(databaseProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
{
    var databaseName = Environment.GetEnvironmentVariable("POKEMON_STORY_DATABASE_NAME")
        ?? builder.Configuration["Database:Name"]
        ?? "PokemonStoryInMemory";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<GoogleAuthenticationOptions>(
    builder.Configuration.GetSection(GoogleAuthenticationOptions.SectionName));
builder.Services.AddHttpClient<IGoogleAccessTokenValidationService, GoogleAccessTokenValidationService>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PokemonDamageCalculatorForStory API",
        Version = "v1",
        Description = "ストーリー攻略用ポケモンダメージ計算 foundation API"
    });

    options.AddSecurityDefinition(GoogleAuthenticationDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Google access token",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Google ログインで取得した access token を指定してください。"
    });
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
    options.AddPolicy(AppPolicies.RunOwner, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(AppPolicies.AdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AppRoles.Administrator);
    });
});

builder.Services.AddScoped<IUserAuthorizationInfoRepository, UserAuthorizationInfoRepository>();
builder.Services.AddScoped<IRulesetRepository, RulesetRepository>();
builder.Services.AddScoped<IRunRepository, RunRepository>();
builder.Services.AddScoped<IShareRepository, ShareRepository>();
builder.Services.AddScoped<IImportJobRepository, ImportJobRepository>();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
builder.Services.AddScoped<StoryProgressionProjector>();
builder.Services.AddScoped<PokemonStoryService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        await Results.Problem(
            statusCode: statusCode,
            title: exception is null ? "Unexpected error" : "Request failed",
            detail: exception?.Message).ExecuteAsync(context);
    });
});

var useHttpsRedirection = ShouldUseHttpsRedirection(app.Configuration);
if (!app.Environment.IsProduction())
{
    await ApplyDatabaseMigrationsAsync(app);
}

await SeedDatabaseAsync(app);

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

var api = app.MapGroup("/api");

api.MapGet("/rulesets", async (PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListRulesetsAsync(cancellationToken)))
    .AllowAnonymous();

api.MapGet("/rulesets/{rulesetId:guid}", async (Guid rulesetId, PokemonStoryService service, CancellationToken cancellationToken) =>
    {
        var ruleset = await service.FindRulesetAsync(rulesetId, cancellationToken);
        return ruleset is null ? Results.NotFound() : Results.Ok(ruleset);
    })
    .AllowAnonymous();

api.MapGet("/master-version-sets/{versionSetId:guid}", async (Guid versionSetId, PokemonStoryService service, CancellationToken cancellationToken) =>
    {
        var versionSet = await service.FindMasterVersionSetAsync(versionSetId, cancellationToken);
        return versionSet is null ? Results.NotFound() : Results.Ok(versionSet);
    })
    .AllowAnonymous();

var runs = api.MapGroup("/runs").RequireAuthorization(AppPolicies.RunOwner);
mapRunEndpoints(runs);

var routes = api.MapGroup("/routes").RequireAuthorization(AppPolicies.RunOwner);
mapRouteEndpoints(routes);

var battles = api.MapGroup("/battles").RequireAuthorization(AppPolicies.RunOwner);
mapBattleEndpoints(battles);

api.MapPost("/calculations/damage", async (DamageCalculationApiRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CalculateDamageAsync(request.ToDomain(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);

api.MapPost("/calculations/compare-patterns", async (ComparePatternsRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
    {
        var patterns = request.Patterns.ToDictionary(
            item => item.PatternKey,
            item => (
                item.MovePowerDelta,
                item.AttackBonus,
                (IReadOnlyList<DamageModifier>)(item.AdditionalModifiers?.Select(modifier => modifier.ToDomain()).ToArray() ?? Array.Empty<DamageModifier>())));

        var result = await service.ComparePatternsAsync(request.BaseCase.ToDomain(), patterns, cancellationToken);
        return Results.Ok(result);
    })
    .RequireAuthorization(AppPolicies.RunOwner);
api.MapPost("/calculations/damage:compare-patterns", async (ComparePatternsRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
    {
        var patterns = request.Patterns.ToDictionary(
            item => item.PatternKey,
            item => (
                item.MovePowerDelta,
                item.AttackBonus,
                (IReadOnlyList<DamageModifier>)(item.AdditionalModifiers?.Select(modifier => modifier.ToDomain()).ToArray() ?? Array.Empty<DamageModifier>())));

        var result = await service.ComparePatternsAsync(request.BaseCase.ToDomain(), patterns, cancellationToken);
        return Results.Ok(result);
    })
    .RequireAuthorization(AppPolicies.RunOwner);

api.MapPost("/calculations/damage:search-thresholds", async (ThresholdSearchApiRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ThresholdSearchAsync(request.ToDomain(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);

api.MapPost("/calculations/threshold-search", async (ThresholdSearchApiRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ThresholdSearchAsync(request.ToDomain(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);

api.MapGet("/presets", async (Guid runId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListPresetsAsync(runId, cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);
api.MapPost("/presets", async (UpsertPresetRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreatePresetAsync(request.ToDomain(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);
api.MapGet("/presets/{presetId:guid}", async (Guid presetId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetPresetAsync(presetId, cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);
api.MapPut("/presets/{presetId:guid}", async (Guid presetId, UpsertPresetRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdatePresetAsync(presetId, request.ToDomain(presetId), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);

var shares = api.MapGroup("/shares");
shares.MapPost("/snapshots", async (CreateShareRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateShareAsync(request.RunId, request.SourceType, request.SourceId, request.Visibility, request.AllowedRoles ?? Array.Empty<string>(), request.Summary, request.FrozenInput?.GetRawText(), request.FrozenOutput?.GetRawText(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);
shares.MapPost(string.Empty, async (CreateShareRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateShareAsync(request.RunId, request.SourceType, request.SourceId, request.Visibility, request.AllowedRoles ?? Array.Empty<string>(), request.Summary, request.FrozenInput?.GetRawText(), request.FrozenOutput?.GetRawText(), cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);
shares.MapGet("/{shareId:guid}", async (Guid shareId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetShareAsync(shareId, cancellationToken)))
    .RequireAuthorization();
shares.MapGet("/{shareId:guid}/comments", async (Guid shareId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListCommentsAsync(shareId, cancellationToken)))
    .RequireAuthorization();
shares.MapPost("/{shareId:guid}/comments", async (Guid shareId, AddCommentRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.AddCommentAsync(shareId, request.RevisionId, request.Body, cancellationToken)))
    .RequireAuthorization();
shares.MapGet("/{shareId:guid}/revisions", async (Guid shareId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListRevisionsAsync(shareId, cancellationToken)))
    .RequireAuthorization();
shares.MapPost("/{shareId:guid}:diff", async (Guid shareId, Guid? baseRevisionId, Guid? targetRevisionId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetShareDiffAsync(shareId, baseRevisionId, targetRevisionId, cancellationToken)))
    .RequireAuthorization();
shares.MapGet("/{shareId:guid}/diff", async (Guid shareId, Guid? baseRevisionId, Guid? targetRevisionId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetShareDiffAsync(shareId, baseRevisionId, targetRevisionId, cancellationToken)))
    .RequireAuthorization();
shares.MapPost("/{shareId:guid}:revise", async (Guid shareId, PublishRevisionRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.PublishRevisionAsync(shareId, request.Summary, cancellationToken)))
    .RequireAuthorization();
shares.MapPost("/{shareId:guid}:publish-revision", async (Guid shareId, PublishRevisionRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.PublishRevisionAsync(shareId, request.Summary, cancellationToken)))
    .RequireAuthorization(AppPolicies.RunOwner);

var admin = api.MapGroup("/admin").RequireAuthorization(AppPolicies.AdminOnly);
admin.MapPost("/imports", async (CreateImportJobRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateImportJobAsync(request.RulesetId, request.WorkbookName, request.Mode, request.SourceType, request.WorkbookContent, cancellationToken)));
admin.MapGet("/imports/{jobId:guid}", async (Guid jobId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetImportJobAsync(jobId, cancellationToken)));
admin.MapPost("/import-jobs:dry-run", async (CreateImportJobRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateDryRunImportJobAsync(request.RulesetId, request.WorkbookName, request.SourceType ?? "spreadsheet", request.WorkbookContent, cancellationToken)));
admin.MapPost("/import-jobs", async (CreateImportJobRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateCommitImportJobAsync(request.RulesetId, request.WorkbookName, request.SourceType ?? "spreadsheet", request.WorkbookContent, cancellationToken)));
admin.MapGet("/import-jobs/{jobId:guid}", async (Guid jobId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetImportJobAsync(jobId, cancellationToken)));
admin.MapGet("/master-version-sets", async (PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListMasterVersionSetsAsync(cancellationToken)));
admin.MapPost("/master-version-sets/{versionSetId:guid}:publish", async (Guid versionSetId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.PublishMasterVersionSetAsync(versionSetId, cancellationToken)));

await app.RunAsync();

static void mapRunEndpoints(RouteGroupBuilder runs)
{
    runs.MapGet(string.Empty, async (PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ListRunsAsync(cancellationToken)));

    runs.MapPost(string.Empty, async (CreateRunRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateRunAsync(request.RulesetId, request.Name, cancellationToken)));

    runs.MapGet("/{runId:guid}", async (Guid runId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetRunAsync(runId, cancellationToken)));

    runs.MapPut("/{runId:guid}", async (Guid runId, UpdateRunRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateRunAsync(runId, request.Name, request.Status, cancellationToken)));

    runs.MapGet("/{runId:guid}/initial-state", async (Guid runId, PokemonStoryService service, CancellationToken cancellationToken) =>
    {
        var initialState = await service.GetInitialStateAsync(runId, cancellationToken);
        return initialState is null ? Results.NotFound() : Results.Ok(initialState);
    });

    runs.MapPut("/{runId:guid}/initial-state", async (Guid runId, UpsertInitialStateRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpsertInitialStateAsync(runId, request.ToDomain(), cancellationToken)));

    runs.MapPost("/{runId:guid}/routes", async (Guid runId, CreateRouteRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateRouteAsync(runId, request.Name, request.SimulatedPartyMemberIds, cancellationToken)));

    runs.MapPost("/{runId:guid}/enemy-groups", async (Guid runId, UpsertEnemyGroupRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.AddEnemyGroupAsync(runId, request.ToDomain(runId), cancellationToken)));

    runs.MapPut("/{runId:guid}/enemy-groups/{groupId:guid}", async (Guid runId, Guid groupId, UpsertEnemyGroupRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateEnemyGroupAsync(runId, groupId, request.ToDomain(runId, groupId), cancellationToken)));

    runs.MapGet("/{runId:guid}/battles:search", async (Guid runId, string? keyword, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.SearchBattlesAsync(runId, keyword, cancellationToken)));

}

static void mapRouteEndpoints(RouteGroupBuilder routes)
{
    routes.MapPost("/{routeId:guid}/events", async (Guid routeId, UpsertProgressionEventRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.AddProgressionEventAsync(routeId, request.ToDomain(), cancellationToken)));

    routes.MapPut("/{routeId:guid}/events/{eventId:guid}", async (Guid routeId, Guid eventId, UpsertProgressionEventRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateProgressionEventAsync(routeId, eventId, request.ToDomain(), cancellationToken)));

    routes.MapPost("/{routeId:guid}:reorder", async (Guid routeId, ReorderRouteRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.ReorderRouteAsync(routeId, request.EventIds, cancellationToken)));

    routes.MapPost("/{routeId:guid}/battles", async (Guid routeId, UpsertBattleRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.AddBattleAsync(routeId, request.ToDomain(routeId), cancellationToken)));

    routes.MapPut("/{routeId:guid}/battles/{battleId:guid}", async (Guid routeId, Guid battleId, UpsertBattleRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateBattleAsync(routeId, battleId, request.ToDomain(routeId, battleId), cancellationToken)));

    routes.MapPost("/{routeId:guid}:verify", async (Guid routeId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.VerifyRouteAsync(routeId, cancellationToken)));

    routes.MapGet("/{routeId:guid}/verification", async (Guid routeId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.VerifyRouteAsync(routeId, cancellationToken)));

    routes.MapGet("/{routeId:guid}/progression", async (Guid routeId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.GetRouteProgressionAsync(routeId, cancellationToken)));

    routes.MapPost("/{routeId:guid}:recalculate", async (Guid routeId, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.RecalculateRouteAsync(routeId, cancellationToken)));
}

static void mapBattleEndpoints(RouteGroupBuilder battles)
{
    battles.MapPut("/{battleId:guid}/participation", async (Guid battleId, UpdateBattleParticipationRequest request, PokemonStoryService service, CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateBattleParticipationAsync(
            battleId,
            request.SuggestedPartyMemberIds?.ToArray() ?? Array.Empty<Guid>(),
            request.Participations.Select(item => item.ToDomain()).ToArray(),
            cancellationToken)));
}

static bool ShouldUseHttpsRedirection(IConfiguration configuration)
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

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
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
                app.Logger.LogInformation("Migration が存在しないため EnsureCreated を実行します。 Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
                await db.Database.EnsureCreatedAsync();
            }

            return;
        }
        catch (SqlException ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "SQL Server is not ready yet. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }
        catch (InvalidOperationException ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "Database migration failed during startup. Attempt {Attempt} of {MaxAttempts}.", attempt, maxAttempts);
        }

        await Task.Delay(delay);
    }
}

static async Task SeedDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbSeeder.SeedAsync(db);
}

public partial class Program;
