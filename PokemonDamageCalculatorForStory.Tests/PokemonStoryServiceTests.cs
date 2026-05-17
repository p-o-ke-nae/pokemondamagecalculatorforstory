using PokemonDamageCalculatorForStory.Application.Identity;
using PokemonDamageCalculatorForStory.Application.Services;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Domain.Ports;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PokemonDamageCalculatorForStory.Tests;

[TestClass]
public sealed class PokemonStoryServiceTests
{
    [TestMethod]
    public async Task VerifyRouteAsync_WithoutInitialState_ReturnsIssue()
    {
        var service = CreateService(out var seededRun);

        var result = await service.VerifyRouteAsync(seededRun.Routes[0].Id, CancellationToken.None);

        Assert.AreEqual(seededRun.Routes[0].Id, result.RouteId);
        Assert.IsTrue(result.Issues.Any(item => item.Code == "initial-state.missing"));
        Assert.IsTrue(result.SourceReferences.Any(item => item.ReferenceType == "ruleset"));
    }

    [TestMethod]
    public async Task CalculateDamageAsync_WithStabAndCritical_IncludesModifierReferences()
    {
        var service = CreateService(out var seededRun, includeInitialState: true);
        var battle = seededRun.Routes[0].Battles[0];

        var result = await service.CalculateDamageAsync(
            new DamageCalculationRequest(
                seededRun.Id,
                seededRun.Routes[0].Id,
                battle.Id,
                "Spark",
                65,
                "Electric",
                new CombatantSnapshot("Pikachu", 18, 41, 26, "Electric", "Magnet"),
                new CombatantSnapshot("Wingull", 14, 20, 18, "Water", null),
                true,
                2m,
                Array.Empty<DamageModifier>()),
            CancellationToken.None);

        Assert.IsTrue(result.MinimumDamage > 0);
        CollectionAssert.Contains(result.AppliedModifiers.Select(item => item.Code).ToList(), "stab");
        CollectionAssert.Contains(result.AppliedModifiers.Select(item => item.Code).ToList(), "critical");
        Assert.IsTrue(result.SourceReferences.Count >= 3);
    }

    private static PokemonStoryService CreateService(out RunAggregate seededRun, bool includeInitialState = false)
    {
        var ruleset = new Ruleset(Guid.Parse("11111111-1111-1111-1111-111111111111"), "gen3-emerald-story", "Gen3", "Pokemon Emerald", "story-v1", "active", "foundation");
        var versionSet = new MasterVersionSet(Guid.Parse("22222222-2222-2222-2222-222222222222"), ruleset.Id, "seed", DateTimeOffset.UtcNow, true, new[] { new SourceReference("seed", "seed", "spreadsheet") });
        var route = new RoutePlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Route 103",
            false,
            "seed",
            new[] { new ProgressionEvent(Guid.NewGuid(), 1, "gain-exp", "Defeat wild battle", 1, 30, "story", 1) },
            new[]
            {
                new BattleDefinition(Guid.NewGuid(), Guid.Empty, "May 1", "arbitrary", "trainer", false, null, null, new[]
                {
                    new EnemyCombatant("Wingull", 14, 36, 20, 18, null)
                }, null)
            },
            null);

        seededRun = new RunAggregate(
            route.RunId,
            "user-1",
            ruleset.Id,
            versionSet.Id,
            "Emerald run",
            "draft",
            includeInitialState ? new RunInitialState(1, "Pikachu", 18, 41, 26, 3200, "Magnet", new[] { "Spark" }, null) : null,
            new[] { route },
            Array.Empty<EnemyGroupDefinition>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        route = route with
        {
            RunId = seededRun.Id,
            Battles = route.Battles.Select(item => item with { RouteId = route.Id }).ToArray()
        };
        seededRun = seededRun with { Routes = new[] { route } };

        return new PokemonStoryService(
            new FakeCurrentUserAccessor(new CurrentUser("user-1", "user-1", "user-1@example.test", new[] { "Member" })),
            new InMemoryRulesetRepository(ruleset, versionSet),
            new InMemoryRunRepository(seededRun),
            new InMemoryShareRepository(),
            new InMemoryImportJobRepository());
    }

    private sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly CurrentUser? _currentUser;

        public FakeCurrentUserAccessor(CurrentUser? currentUser)
        {
            _currentUser = currentUser;
        }

        public CurrentUser? GetCurrentUser() => _currentUser;
    }

    private sealed class InMemoryRulesetRepository : IRulesetRepository
    {
        private readonly Dictionary<Guid, Ruleset> _rulesets;
        private readonly Dictionary<Guid, MasterVersionSet> _versionSets;

        public InMemoryRulesetRepository(Ruleset ruleset, MasterVersionSet versionSet)
        {
            _rulesets = new Dictionary<Guid, Ruleset> { [ruleset.Id] = ruleset };
            _versionSets = new Dictionary<Guid, MasterVersionSet> { [versionSet.Id] = versionSet };
        }

        public Task<Ruleset?> FindRulesetAsync(Guid rulesetId, CancellationToken cancellationToken = default)
            => Task.FromResult(_rulesets.TryGetValue(rulesetId, out var value) ? value : null);

        public Task<MasterVersionSet?> FindMasterVersionSetAsync(Guid versionSetId, CancellationToken cancellationToken = default)
            => Task.FromResult(_versionSets.TryGetValue(versionSetId, out var value) ? value : null);

        public Task<IReadOnlyList<Ruleset>> ListRulesetsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Ruleset>>(_rulesets.Values.ToArray());

        public Task<IReadOnlyList<MasterVersionSet>> ListMasterVersionSetsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MasterVersionSet>>(_versionSets.Values.ToArray());

        public Task SaveMasterVersionSetAsync(MasterVersionSet versionSet, CancellationToken cancellationToken = default)
        {
            _versionSets[versionSet.Id] = versionSet;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRunRepository : IRunRepository
    {
        private readonly Dictionary<Guid, RunAggregate> _runs;

        public InMemoryRunRepository(RunAggregate run)
        {
            _runs = new Dictionary<Guid, RunAggregate> { [run.Id] = run };
        }

        public Task<RunAggregate?> FindRunAsync(Guid runId, CancellationToken cancellationToken = default)
            => Task.FromResult(_runs.TryGetValue(runId, out var value) ? value : null);

        public Task<IReadOnlyList<RunAggregate>> ListRunsAsync(string ownerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RunAggregate>>(_runs.Values.Where(item => item.OwnerUserId == ownerUserId).ToArray());

        public Task SaveRunAsync(RunAggregate run, CancellationToken cancellationToken = default)
        {
            _runs[run.Id] = run;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryShareRepository : IShareRepository
    {
        private readonly Dictionary<Guid, SharedRouteSnapshot> _shares = new();

        public Task<SharedRouteSnapshot?> FindShareAsync(Guid shareId, CancellationToken cancellationToken = default)
            => Task.FromResult(_shares.TryGetValue(shareId, out var value) ? value : null);

        public Task SaveShareAsync(SharedRouteSnapshot share, CancellationToken cancellationToken = default)
        {
            _shares[share.Id] = share;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryImportJobRepository : IImportJobRepository
    {
        private readonly Dictionary<Guid, ImportJob> _jobs = new();

        public Task<ImportJob?> FindJobAsync(Guid jobId, CancellationToken cancellationToken = default)
            => Task.FromResult(_jobs.TryGetValue(jobId, out var value) ? value : null);

        public Task SaveJobAsync(ImportJob job, CancellationToken cancellationToken = default)
        {
            _jobs[job.Id] = job;
            return Task.CompletedTask;
        }
    }
}
