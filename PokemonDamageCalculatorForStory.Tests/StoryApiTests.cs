using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Tests.TestDoubles;

namespace PokemonDamageCalculatorForStory.Tests;

[TestClass]
public sealed class StoryApiTests
{
    private static readonly Guid SeedRulesetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestWebApplicationFactory _factory = new();

    [TestMethod]
    public async Task StoryFoundationEndpoints_WorkEndToEnd()
    {
        using var client = CreateClient("story-user");

        var rulesets = await client.GetFromJsonAsync<List<Ruleset>>("/api/rulesets");
        Assert.IsNotNull(rulesets);
        Assert.AreEqual(SeedRulesetId, rulesets[0].Id);

        var runResponse = await client.PostAsJsonAsync("/api/runs", new { rulesetId = SeedRulesetId, name = "Emerald foundation run" });
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<RunAggregate>();
        Assert.IsNotNull(run);

        var initialStateResponse = await client.PutAsJsonAsync($"/api/runs/{run.Id}/initial-state", new
        {
            playerSpecies = "Pikachu",
            level = 18,
            attack = 41,
            defense = 26,
            money = 3200,
            heldItem = "Magnet",
            moves = new[] { "Spark", "Quick Attack" },
            memo = "Gym 2 prep"
        });
        initialStateResponse.EnsureSuccessStatusCode();

        var routeResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/routes", new { name = "Route 103" });
        routeResponse.EnsureSuccessStatusCode();
        var route = await routeResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);

        var enemyGroupResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/enemy-groups", new
        {
            name = "May opening",
            sourceKind = "custom",
            members = new[]
            {
                new { species = "Wingull", level = 14, hp = 36, attack = 20, defense = 18, note = "lead" }
            }
        });
        enemyGroupResponse.EnsureSuccessStatusCode();
        var enemyGroup = await enemyGroupResponse.Content.ReadFromJsonAsync<EnemyGroupDefinition>();
        Assert.IsNotNull(enemyGroup);

        var battleResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/battles", new
        {
            title = "May 1",
            sourceKind = "custom-group",
            battleKind = "trainer",
            isOptional = false,
            masterBattleCode = (string?)null,
            enemyGroupId = enemyGroup.Id,
            inlineEnemies = Array.Empty<object>(),
            notes = "Custom trainer ref"
        });
        battleResponse.EnsureSuccessStatusCode();
        var battle = await battleResponse.Content.ReadFromJsonAsync<BattleDefinition>();
        Assert.IsNotNull(battle);

        var searchHits = await client.GetFromJsonAsync<List<BattleSearchHit>>($"/api/runs/{run.Id}/battles:search?keyword=Wingull");
        Assert.IsNotNull(searchHits);
        Assert.AreEqual(1, searchHits.Count);

        var verifyResponse = await client.PostAsync($"/api/routes/{route.Id}:verify", content: null);
        verifyResponse.EnsureSuccessStatusCode();
        var verification = await verifyResponse.Content.ReadFromJsonAsync<RouteVerificationResult>();
        Assert.IsNotNull(verification);
        Assert.AreEqual(0, verification.Issues.Count);

        var damageResponse = await client.PostAsJsonAsync("/api/calculations/damage", new
        {
            runId = run.Id,
            routeId = route.Id,
            battleId = battle.Id,
            moveName = "Spark",
            movePower = 65,
            moveType = "Electric",
            attacker = new { species = "Pikachu", level = 18, attack = 41, defense = 26, primaryType = "Electric", heldItem = "Magnet" },
            defender = new { species = "Wingull", level = 14, attack = 20, defense = 18, primaryType = "Water", heldItem = (string?)null },
            isCritical = true,
            typeEffectiveness = 2.0m,
            additionalModifiers = new[]
            {
                new { code = "item", label = "Magnet", multiplier = 1.1m, sourceCode = "item.magnet", sourceLabel = "Magnet", sourceType = "item" }
            }
        });
        damageResponse.EnsureSuccessStatusCode();
        var damage = await damageResponse.Content.ReadFromJsonAsync<DamageCalculationResult>();
        Assert.IsNotNull(damage);
        Assert.IsTrue(damage.MaximumDamage >= damage.MinimumDamage);

        var compareResponse = await client.PostAsJsonAsync("/api/calculations/compare-patterns", new
        {
            baseCase = new
            {
                runId = run.Id,
                routeId = route.Id,
                battleId = battle.Id,
                moveName = "Spark",
                movePower = 65,
                moveType = "Electric",
                attacker = new { species = "Pikachu", level = 18, attack = 41, defense = 26, primaryType = "Electric", heldItem = "Magnet" },
                defender = new { species = "Wingull", level = 14, attack = 20, defense = 18, primaryType = "Water", heldItem = (string?)null },
                isCritical = false,
                typeEffectiveness = 2.0m,
                additionalModifiers = Array.Empty<object>()
            },
            patterns = new[]
            {
                new { patternKey = "attack+2", movePowerDelta = 0, attackBonus = 2, additionalModifiers = Array.Empty<object>() }
            }
        });
        compareResponse.EnsureSuccessStatusCode();
        var compareResults = await compareResponse.Content.ReadFromJsonAsync<List<ComparisonPatternResult>>();
        Assert.IsNotNull(compareResults);
        Assert.AreEqual(1, compareResults.Count);

        var thresholdResponse = await client.PostAsJsonAsync("/api/calculations/threshold-search", new
        {
            runId = run.Id,
            routeId = route.Id,
            battleId = battle.Id,
            moveName = "Spark",
            moveType = "Electric",
            movePowerRangeStart = 50,
            movePowerRangeEnd = 70,
            maximumAttackBonus = 3,
            targetMinimumDamage = 20
        });
        thresholdResponse.EnsureSuccessStatusCode();
        var threshold = await thresholdResponse.Content.ReadFromJsonAsync<ThresholdSearchResult>();
        Assert.IsNotNull(threshold);
        Assert.IsTrue(threshold.Candidates.Count > 0);
    }

    [TestMethod]
    public async Task ShareAndImportEndpoints_WorkWithRevisionAndAdminBoundaries()
    {
        using var client = CreateClient("share-user");
        var run = await CreateRunWithBattleAsync(client);
        var route = run.Routes[0];

        var shareResponse = await client.PostAsJsonAsync("/api/shares", new
        {
            runId = run.Id,
            routeId = route.Id,
            visibility = "unlisted",
            summary = "foundation publish"
        });
        shareResponse.EnsureSuccessStatusCode();
        var share = await shareResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(share);

        using var anonymousClient = _factory.CreateClient();
        var anonymousReadResponse = await anonymousClient.GetAsync($"/api/shares/{share.Id}");
        Assert.AreEqual(HttpStatusCode.Forbidden, anonymousReadResponse.StatusCode);

        using var memberClient = CreateClient("member-user");
        var otherUserCommentResponse = await memberClient.PostAsJsonAsync($"/api/shares/{share.Id}/comments", new { revisionId = share.CurrentRevisionId, body = "Intruding comment" });
        Assert.AreEqual(HttpStatusCode.Forbidden, otherUserCommentResponse.StatusCode);

        var commentResponse = await client.PostAsJsonAsync($"/api/shares/{share.Id}/comments", new { revisionId = share.CurrentRevisionId, body = "Looks good." });
        commentResponse.EnsureSuccessStatusCode();
        var commentedShare = await commentResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(commentedShare);
        Assert.AreEqual(1, commentedShare.Comments.Count);

        var eventResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/events", new
        {
            eventType = "rare-candy",
            summary = "Use Rare Candy",
            levelDelta = 1,
            moneyDelta = 0,
            sourceReference = "item"
        });
        eventResponse.EnsureSuccessStatusCode();

        var publishRevisionResponse = await client.PostAsJsonAsync($"/api/shares/{share.Id}:publish-revision", new { summary = "after rare candy" });
        publishRevisionResponse.EnsureSuccessStatusCode();
        var revisedShare = await publishRevisionResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(revisedShare);
        Assert.AreEqual(2, revisedShare.Revisions.Count);

        var diff = await client.GetFromJsonAsync<ShareDiffResult>($"/api/shares/{share.Id}/diff?baseRevisionId={revisedShare.BaseRevisionId}&targetRevisionId={revisedShare.CurrentRevisionId}");
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.ChangedFields.Count > 0);

        var forbiddenResponse = await memberClient.PostAsJsonAsync("/api/admin/import-jobs:dry-run", new { rulesetId = SeedRulesetId, workbookName = "emerald.xlsx" });
        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var adminClient = CreateClient("admin-user", "Administrator");
        var importResponse = await adminClient.PostAsJsonAsync("/api/admin/import-jobs", new { rulesetId = SeedRulesetId, workbookName = "emerald.xlsx" });
        importResponse.EnsureSuccessStatusCode();
        var job = await importResponse.Content.ReadFromJsonAsync<ImportJob>();
        Assert.IsNotNull(job);
        Assert.AreEqual("commit", job.Mode);

        var jobLookup = await adminClient.GetFromJsonAsync<ImportJob>($"/api/admin/import-jobs/{job.Id}");
        Assert.IsNotNull(jobLookup);

        var masterVersionSets = await adminClient.GetFromJsonAsync<List<MasterVersionSet>>("/api/admin/master-version-sets");
        Assert.IsNotNull(masterVersionSets);
        Assert.IsTrue(masterVersionSets.Count >= 2);
    }

    private HttpClient CreateClient(string userId, string role = "Member")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task<RunAggregate> CreateRunWithBattleAsync(HttpClient client)
    {
        var runResponse = await client.PostAsJsonAsync("/api/runs", new { rulesetId = SeedRulesetId, name = "Share run" });
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<RunAggregate>() ?? throw new InvalidOperationException();

        var initialStateResponse = await client.PutAsJsonAsync($"/api/runs/{run.Id}/initial-state", new
        {
            playerSpecies = "Torchic",
            level = 15,
            attack = 36,
            defense = 24,
            money = 2100,
            heldItem = "Charcoal",
            moves = new[] { "Ember" },
            memo = "share fixture"
        });
        initialStateResponse.EnsureSuccessStatusCode();
        run = await initialStateResponse.Content.ReadFromJsonAsync<RunAggregate>() ?? throw new InvalidOperationException();

        var routeResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/routes", new { name = "Route 102" });
        routeResponse.EnsureSuccessStatusCode();
        var route = await routeResponse.Content.ReadFromJsonAsync<RoutePlan>() ?? throw new InvalidOperationException();

        var battleResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/battles", new
        {
            title = "Youngster Calvin",
            sourceKind = "arbitrary",
            battleKind = "trainer",
            isOptional = false,
            masterBattleCode = (string?)null,
            enemyGroupId = (Guid?)null,
            inlineEnemies = new[]
            {
                new { species = "Poochyena", level = 7, hp = 20, attack = 15, defense = 12, note = "fixture" }
            },
            notes = "share battle"
        });
        battleResponse.EnsureSuccessStatusCode();
        var battle = await battleResponse.Content.ReadFromJsonAsync<BattleDefinition>() ?? throw new InvalidOperationException();

        return run with
        {
            Routes = new[]
            {
                route with { Battles = new[] { battle } }
            }
        };
    }
}
