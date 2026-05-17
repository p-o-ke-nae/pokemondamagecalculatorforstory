using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

/// <summary>ruleset 永続化を行う EF Core 実装です。</summary>
public sealed class RulesetRepository : IRulesetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;

    /// <summary>リポジトリを初期化します。</summary>
    public RulesetRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ruleset>> ListRulesetsAsync(CancellationToken cancellationToken = default)
        => (await _context.Rulesets
            .OrderBy(item => item.Slug)
            .ToListAsync(cancellationToken))
            .Select(item => Deserialize<Ruleset>(item.PayloadJson))
            .ToArray();

    /// <inheritdoc />
    public async Task<Ruleset?> FindRulesetAsync(Guid rulesetId, CancellationToken cancellationToken = default)
        => (await _context.Rulesets.SingleOrDefaultAsync(item => item.Id == rulesetId, cancellationToken)) is { } persisted
            ? Deserialize<Ruleset>(persisted.PayloadJson)
            : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MasterVersionSet>> ListMasterVersionSetsAsync(CancellationToken cancellationToken = default)
        => (await _context.MasterVersionSets
            .OrderByDescending(item => item.Id)
            .ToListAsync(cancellationToken))
            .Select(item => Deserialize<MasterVersionSet>(item.PayloadJson))
            .ToArray();

    /// <inheritdoc />
    public async Task<MasterVersionSet?> FindMasterVersionSetAsync(Guid versionSetId, CancellationToken cancellationToken = default)
        => (await _context.MasterVersionSets.SingleOrDefaultAsync(item => item.Id == versionSetId, cancellationToken)) is { } persisted
            ? Deserialize<MasterVersionSet>(persisted.PayloadJson)
            : null;

    /// <inheritdoc />
    public async Task SaveMasterVersionSetAsync(MasterVersionSet versionSet, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.MasterVersionSets.SingleOrDefaultAsync(item => item.Id == versionSet.Id, cancellationToken);
        if (persisted is null)
        {
            _context.MasterVersionSets.Add(new PersistedMasterVersionSet
            {
                Id = versionSet.Id,
                RulesetId = versionSet.RulesetId,
                IsPublished = versionSet.IsPublished,
                PayloadJson = JsonSerializer.Serialize(versionSet, JsonOptions)
            });
        }
        else
        {
            persisted.RulesetId = versionSet.RulesetId;
            persisted.IsPublished = versionSet.IsPublished;
            persisted.PayloadJson = JsonSerializer.Serialize(versionSet, JsonOptions);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    internal async Task SeedRulesetAsync(Ruleset ruleset, CancellationToken cancellationToken = default)
    {
        if (await _context.Rulesets.AnyAsync(item => item.Id == ruleset.Id, cancellationToken))
        {
            return;
        }

        _context.Rulesets.Add(new PersistedRuleset
        {
            Id = ruleset.Id,
            Slug = ruleset.Slug,
            PayloadJson = JsonSerializer.Serialize(ruleset, JsonOptions)
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"JSON を {typeof(T).Name} に復元できません。");
}

/// <summary>run 永続化を行う EF Core 実装です。</summary>
public sealed class RunRepository : IRunRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;

    /// <summary>リポジトリを初期化します。</summary>
    public RunRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RunAggregate>> ListRunsAsync(string ownerUserId, CancellationToken cancellationToken = default)
        => (await _context.Runs
            .Where(item => item.OwnerUserId == ownerUserId)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken))
            .Select(item => Deserialize<RunAggregate>(item.PayloadJson))
            .ToArray();

    /// <inheritdoc />
    public async Task<RunAggregate?> FindRunAsync(Guid runId, CancellationToken cancellationToken = default)
        => (await _context.Runs.SingleOrDefaultAsync(item => item.Id == runId, cancellationToken)) is { } persisted
            ? Deserialize<RunAggregate>(persisted.PayloadJson)
            : null;

    /// <inheritdoc />
    public async Task SaveRunAsync(RunAggregate run, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.Runs.SingleOrDefaultAsync(item => item.Id == run.Id, cancellationToken);
        if (persisted is null)
        {
            _context.Runs.Add(new PersistedRun
            {
                Id = run.Id,
                OwnerUserId = run.OwnerUserId,
                PayloadJson = JsonSerializer.Serialize(run, JsonOptions)
            });
        }
        else
        {
            persisted.OwnerUserId = run.OwnerUserId;
            persisted.PayloadJson = JsonSerializer.Serialize(run, JsonOptions);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"JSON を {typeof(T).Name} に復元できません。");
}

/// <summary>share 永続化を行う EF Core 実装です。</summary>
public sealed class ShareRepository : IShareRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;

    /// <summary>リポジトリを初期化します。</summary>
    public ShareRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<SharedRouteSnapshot?> FindShareAsync(Guid shareId, CancellationToken cancellationToken = default)
        => (await _context.Shares.SingleOrDefaultAsync(item => item.Id == shareId, cancellationToken)) is { } persisted
            ? Deserialize<SharedRouteSnapshot>(persisted.PayloadJson)
            : null;

    /// <inheritdoc />
    public async Task SaveShareAsync(SharedRouteSnapshot share, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.Shares.SingleOrDefaultAsync(item => item.Id == share.Id, cancellationToken);
        if (persisted is null)
        {
            _context.Shares.Add(new PersistedShare
            {
                Id = share.Id,
                OwnerUserId = share.OwnerUserId,
                PayloadJson = JsonSerializer.Serialize(share, JsonOptions)
            });
        }
        else
        {
            persisted.OwnerUserId = share.OwnerUserId;
            persisted.PayloadJson = JsonSerializer.Serialize(share, JsonOptions);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"JSON を {typeof(T).Name} に復元できません。");
}

/// <summary>import job 永続化を行う EF Core 実装です。</summary>
public sealed class ImportJobRepository : IImportJobRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _context;

    /// <summary>リポジトリを初期化します。</summary>
    public ImportJobRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ImportJob?> FindJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        => (await _context.ImportJobs.SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken)) is { } persisted
            ? Deserialize<ImportJob>(persisted.PayloadJson)
            : null;

    /// <inheritdoc />
    public async Task SaveJobAsync(ImportJob job, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.ImportJobs.SingleOrDefaultAsync(item => item.Id == job.Id, cancellationToken);
        if (persisted is null)
        {
            _context.ImportJobs.Add(new PersistedImportJob
            {
                Id = job.Id,
                SubmittedByUserId = job.SubmittedByUserId,
                PayloadJson = JsonSerializer.Serialize(job, JsonOptions)
            });
        }
        else
        {
            persisted.SubmittedByUserId = job.SubmittedByUserId;
            persisted.PayloadJson = JsonSerializer.Serialize(job, JsonOptions);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"JSON を {typeof(T).Name} に復元できません。");
}
