using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokemonDamageCalculatorForStory.Application.Identity;
using PokemonDamageCalculatorForStory.Application.Services;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Domain.Ports;

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
    public async Task CalculateDamageAsync_WithCentralizedTypeChartAndPpWarning_IncludesDerivedData()
    {
        var service = CreateService(out var seededRun, includeInitialState: true, negativePp: true);
        var battle = seededRun.Routes[0].Battles[0];
        var partyMember = seededRun.InitialState!.BaselineParty[0];

        var result = await service.CalculateDamageAsync(
            new DamageCalculationRequest(
                seededRun.Id,
                seededRun.Routes[0].Id,
                battle.Id,
                partyMember.PartyMemberId,
                null,
                "Spark",
                65,
                PokemonType.Electric,
                new CombatantSnapshot("Pikachu", 18, 41, 26, PokemonType.Electric, null, "Magnet"),
                new CombatantSnapshot("Wingull", 14, 20, 18, PokemonType.Water, PokemonType.Flying, null),
                true,
                null,
                Array.Empty<DamageModifier>()),
            CancellationToken.None);

        Assert.IsGreaterThan(0, result.MinimumDamage);
        CollectionAssert.Contains(result.AppliedModifiers.Select(item => item.Code).ToList(), "stab");
        CollectionAssert.Contains(result.AppliedModifiers.Select(item => item.Code).ToList(), "critical");
        Assert.AreEqual(4m, result.AppliedModifiers.Single(item => item.Code == "effectiveness").Multiplier);
        Assert.IsNotEmpty(result.Warnings);
    }

    [TestMethod]
    public async Task ThresholdSearchAsync_WithUnsupportedConditionMode_Throws()
    {
        var service = CreateService(out var seededRun, includeInitialState: true);
        var battle = seededRun.Routes[0].Battles[0];
        var partyMember = seededRun.InitialState!.BaselineParty[0];

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await service.ThresholdSearchAsync(
                new ThresholdSearchRequest(
                    seededRun.Id,
                    seededRun.Routes[0].Id,
                    battle.Id,
                    partyMember.PartyMemberId,
                    null,
                    "Spark",
                    65,
                    PokemonType.Electric,
                    new[] { "attack" },
                    "anyOf",
                    new[] { new ThresholdCondition("min", "minimum-damage-at-least", 20) },
                    Array.Empty<string>()),
                CancellationToken.None));
    }

    [TestMethod]
    public async Task ThresholdSearchAsync_WithSpeedCondition_ReturnsSolved()
    {
        var service = CreateService(out var seededRun, includeInitialState: true);
        var battle = seededRun.Routes[0].Battles[0];
        var partyMember = seededRun.InitialState!.BaselineParty[0];

        var result = await service.ThresholdSearchAsync(
            new ThresholdSearchRequest(
                seededRun.Id,
                seededRun.Routes[0].Id,
                battle.Id,
                partyMember.PartyMemberId,
                null,
                "Spark",
                65,
                PokemonType.Electric,
                new[] { "speed" },
                "allOf",
                new[] { new ThresholdCondition("speed-check", "action-order-at-least", 40) },
                Array.Empty<string>()),
            CancellationToken.None);

        Assert.AreEqual("solved", result.Status);
        Assert.IsNotNull(result.BestSolution);
        Assert.IsNotEmpty(result.AllMinimalSolutions);
    }

    private static PokemonStoryService CreateService(out RunAggregate seededRun, bool includeInitialState = false, bool negativePp = false)
    {
        var ruleset = new Ruleset(Guid.Parse("11111111-1111-1111-1111-111111111111"), "gen3-emerald-story", "Gen3", "Pokemon Emerald", "story-v1", "active", "foundation");
        var versionSet = new MasterVersionSet(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ruleset.Id,
            "seed",
            DateTimeOffset.UtcNow,
            true,
            new VersionCatalog(
                "damage-v1",
                "experience-v1",
                "pokemon-master-v1",
                "move-master-v1",
                "ability-master-v1",
                "item-master-v1",
                "type-chart-v1",
                "nature-master-v1",
                "story-enemy-master-v1",
                "experience-table-v1",
                "effort-value-master-v1",
                "pp-rule-v1",
                Array.Empty<string>(),
                Array.Empty<Guid>()),
            new[] { new SourceReference("seed", "seed", "spreadsheet") });
        var route = new RoutePlan(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Route 103",
            false,
            includeInitialState ? 1 : 0,
            Array.Empty<int>(),
            "seed",
            Array.Empty<ProgressionEvent>(),
            new[]
            {
                new BattleDefinition(
                    Guid.NewGuid(),
                    Guid.Empty,
                    "May 1",
                    "arbitrary",
                    "trainer",
                    false,
                    null,
                    null,
                    new[]
                    {
                        new EnemyCombatant("Wingull", 14, 36, 20, 18, new PokemonTypeSlot(PokemonType.Water, PokemonType.Flying), 64, new StatValues(0, 0, 0, 0, 0, 1), null)
                    },
                    Array.Empty<Guid>(),
                    Array.Empty<BattleParticipationPlan>(),
                    null)
            },
            Array.Empty<Guid>(),
            null);

        seededRun = new RunAggregate(
            route.RunId,
            "user-1",
            ruleset.Id,
            versionSet.Id,
            "Emerald run",
            "draft",
            includeInitialState
                ? new RunInitialState(
                    1,
                    3200,
                    new[]
                    {
                        new PartyMemberDefinition(
                            Guid.Parse("33333333-3333-3333-3333-333333333333"),
                            1,
                            "Pikachu",
                            18,
                            5832,
                            new StatValues(31, 31, 31, 31, 31, 31),
                            new StatValues(0, 0, 0, 0, 0, 0),
                            "Timid",
                            "Static",
                            new CombatStatSnapshot(50, 41, 26, 30, 30, 40),
                            new PokemonTypeSlot(PokemonType.Electric, null),
                            "Magnet",
                            new[] { new MoveState("Spark", 20, negativePp ? -1 : 10) },
                            null,
                            true)
                    },
                    null)
                : null,
            new[] { route },
            Array.Empty<EnemyGroupDefinition>(),
            Array.Empty<CalculationPreset>(),
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
            new InMemoryImportJobRepository(),
            new StoryProgressionProjector());
    }

    private sealed class FakeCurrentUserAccessor(CurrentUser? currentUser) : ICurrentUserAccessor
    {
        public CurrentUser? GetCurrentUser() => currentUser;
    }

    private sealed class InMemoryRulesetRepository(Ruleset ruleset, MasterVersionSet versionSet) : IRulesetRepository
    {
        private readonly Dictionary<Guid, Ruleset> _rulesets = new() { [ruleset.Id] = ruleset };
        private readonly Dictionary<Guid, MasterVersionSet> _versionSets = new() { [versionSet.Id] = versionSet };

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

    private sealed class InMemoryRunRepository(RunAggregate run) : IRunRepository
    {
        private readonly Dictionary<Guid, RunAggregate> _runs = new() { [run.Id] = run };

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
