using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task SwaggerDocument_ContainsUseCaseAndInputExamples()
    {
        await using var swaggerFactory = new TestWebApplicationFactory("Testing");
        using var client = swaggerFactory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var postRunOperation = document.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Select(operation => operation.Value)
            .Single(operation =>
                operation.TryGetProperty("summary", out var summary)
                && summary.GetString() == "run を作成");

        Assert.AreEqual("run を作成", postRunOperation.GetProperty("summary").GetString());

        var description = postRunOperation.GetProperty("description").GetString();
        Assert.IsNotNull(description);
        StringAssert.Contains(description, "### ユースケース");
        StringAssert.Contains(description, "### 入力例");
        StringAssert.Contains(description, "\"rulesetId\": \"11111111-1111-1111-1111-111111111111\"");
    }

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

        var pikachuId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var bulbasaurId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var initialStateResponse = await client.PutAsJsonAsync($"/api/runs/{run.Id}/initial-state", new
        {
            baselineMoney = 3200,
            baselineParty = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    slot = 1,
                    species = "Pikachu",
                    level = 18,
                    experience = 5832,
                    individualValues = new { hp = 31, attack = 31, defense = 31, specialAttack = 31, specialDefense = 31, speed = 31 },
                    effortValues = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    nature = "Timid",
                    ability = "Static",
                    combatStats = new { hp = 50, attack = 41, defense = 26, specialAttack = 30, specialDefense = 30, speed = 40 },
                    typing = new { primaryType = "Electric", secondaryType = (string?)null },
                    heldItem = "Magnet",
                    moves = new[] { new { moveName = "Spark", maxPp = 20, currentPp = 10 } },
                    memo = "Gym 2 prep",
                    isBattleSimulatorEnabled = true
                },
                new
                {
                    partyMemberId = bulbasaurId,
                    slot = 2,
                    species = "Bulbasaur",
                    level = 12,
                    experience = 1728,
                    individualValues = new { hp = 20, attack = 20, defense = 20, specialAttack = 20, specialDefense = 20, speed = 20 },
                    effortValues = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    nature = "Calm",
                    ability = "Overgrow",
                    combatStats = new { hp = 40, attack = 24, defense = 24, specialAttack = 28, specialDefense = 28, speed = 22 },
                    typing = new { primaryType = "Grass", secondaryType = "Poison" },
                    heldItem = (string?)null,
                    moves = new[] { new { moveName = "Vine Whip", maxPp = 25, currentPp = 25 } },
                    memo = "Reserve member",
                    isBattleSimulatorEnabled = false
                }
            },
            memo = "Gym 2 prep"
        });
        initialStateResponse.EnsureSuccessStatusCode();

        var routeResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/routes", new { name = "Route 103", simulatedPartyMemberIds = new[] { pikachuId } });
        routeResponse.EnsureSuccessStatusCode();
        var route = await routeResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);
        Assert.AreEqual(1, route.InitialStateRevision);
        Assert.IsEmpty(route.OrderedProgressionEventRevisions);

        var enemyGroupResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/enemy-groups", new
        {
            name = "May opening",
            sourceKind = "custom",
            members = new[]
            {
                new
                {
                    species = "Wingull",
                    level = 14,
                    hp = 36,
                    attack = 20,
                    defense = 18,
                    primaryType = "Water",
                    secondaryType = "Flying",
                    baseExperienceYield = 64,
                    effortValueYield = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 1 },
                    note = "lead"
                }
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
            suggestedPartyMemberIds = new[] { pikachuId, bulbasaurId },
            participations = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    participationMode = "active",
                    shareRatio = 1.0m,
                    suggestedRole = "lead",
                    outcomeChecklist = new { sentOutAndDefeated = true, defeatedWhileInReserve = false, didNotDefeat = false, intentionalLoss = false }
                },
                new
                {
                    partyMemberId = bulbasaurId,
                    participationMode = "reserve",
                    shareRatio = 0.5m,
                    suggestedRole = "reserve",
                    outcomeChecklist = new { sentOutAndDefeated = false, defeatedWhileInReserve = true, didNotDefeat = false, intentionalLoss = false }
                }
            },
            notes = "Custom trainer ref"
        });
        battleResponse.EnsureSuccessStatusCode();
        var battle = await battleResponse.Content.ReadFromJsonAsync<BattleDefinition>();
        Assert.IsNotNull(battle);

        var updateParticipationResponse = await client.PutAsJsonAsync($"/api/battles/{battle.Id}/participation", new
        {
            suggestedPartyMemberIds = new[] { pikachuId, bulbasaurId },
            participations = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    participationMode = "shared",
                    shareRatio = 0.75m,
                    suggestedRole = "lead",
                    outcomeChecklist = new { sentOutAndDefeated = true, defeatedWhileInReserve = false, didNotDefeat = false, intentionalLoss = false }
                },
                new
                {
                    partyMemberId = bulbasaurId,
                    participationMode = "reserve",
                    shareRatio = 0.25m,
                    suggestedRole = "reserve",
                    outcomeChecklist = new { sentOutAndDefeated = false, defeatedWhileInReserve = true, didNotDefeat = false, intentionalLoss = false }
                }
            }
        });
        updateParticipationResponse.EnsureSuccessStatusCode();

        var eventResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/events", new
        {
            eventType = "battle-result",
            summary = "May 1 clear",
            linkedBattleId = battle.Id,
            moneyDelta = 300,
            sourceReference = "story",
            partyDeltas = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    experienceDelta = 0,
                    effortValueDelta = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    ppDeltas = new[] { new { moveName = "Spark", delta = -2 } },
                    rareCandyLevels = 0,
                    speciesOverride = (string?)null,
                    abilityOverride = (string?)null,
                    natureOverride = (string?)null,
                    heldItemOverride = (string?)null,
                    replaceMoves = (object[]?)null,
                    notes = "Spark twice"
                }
            }
        });
        eventResponse.EnsureSuccessStatusCode();
        route = await eventResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);
        Assert.IsNotEmpty(route.OrderedProgressionEventRevisions);
        var firstFingerprint = route.ProgressionFingerprint;
        var firstEvent = route.Events.Single();

        var moveChangeResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/events", new
        {
            eventType = "move-change",
            summary = "Learn Thunderbolt",
            linkedBattleId = (Guid?)null,
            moneyDelta = 0,
            sourceReference = "tm24",
            partyDeltas = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    experienceDelta = 0,
                    effortValueDelta = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    ppDeltas = Array.Empty<object>(),
                    rareCandyLevels = 1,
                    speciesOverride = (string?)null,
                    abilityOverride = (string?)null,
                    natureOverride = (string?)null,
                    heldItemOverride = "Quick Claw",
                    replaceMoves = new[] { new { moveName = "Thunderbolt", maxPp = 15, currentPp = 15 } },
                    notes = "TM update"
                }
            }
        });
        moveChangeResponse.EnsureSuccessStatusCode();
        route = await moveChangeResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);
        Assert.AreNotEqual(firstFingerprint, route.ProgressionFingerprint);

        var secondEvent = route.Events.Single(item => item.EventType == "move-change");
        var updateEventResponse = await client.PutAsJsonAsync($"/api/routes/{route.Id}/events/{firstEvent.Id}", new
        {
            eventType = "item-use",
            summary = "May 1 clear updated",
            linkedBattleId = battle.Id,
            moneyDelta = 300,
            sourceReference = "story",
            partyDeltas = new object[]
            {
                new
                {
                    partyMemberId = pikachuId,
                    experienceDelta = 0,
                    effortValueDelta = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    ppDeltas = new[] { new { moveName = "Spark", delta = -3 } },
                    rareCandyLevels = 0,
                    speciesOverride = (string?)null,
                    abilityOverride = (string?)null,
                    natureOverride = (string?)null,
                    heldItemOverride = "Magnet",
                    replaceMoves = (object[]?)null,
                    notes = "Spark three times"
                }
            }
        });
        updateEventResponse.EnsureSuccessStatusCode();
        route = await updateEventResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);
        Assert.IsGreaterThan(firstEvent.Revision, route.Events.Single(item => item.Id == firstEvent.Id).Revision);

        var reorderResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}:reorder", new { eventIds = new[] { secondEvent.Id, firstEvent.Id } });
        reorderResponse.EnsureSuccessStatusCode();
        route = await reorderResponse.Content.ReadFromJsonAsync<RoutePlan>();
        Assert.IsNotNull(route);
        CollectionAssert.AreEqual(new[] { secondEvent.Id, firstEvent.Id }, route.Events.OrderBy(item => item.Sequence).Select(item => item.Id).ToArray());
        Assert.HasCount(2, route.OrderedProgressionEventRevisions);

        var searchHits = await client.GetFromJsonAsync<List<BattleSearchHit>>($"/api/runs/{run.Id}/battles:search?keyword=Wingull");
        Assert.IsNotNull(searchHits);
        Assert.HasCount(1, searchHits);

        var progression = await client.GetFromJsonAsync<RouteProgressionProjection>($"/api/routes/{route.Id}/progression");
        Assert.IsNotNull(progression);
        Assert.HasCount(2, progression.PartyMembers);
        Assert.AreEqual("shared", progression.PartyMembers.Single(item => item.PartyMemberId == pikachuId).SimulationScope);
        Assert.AreEqual("reserve", progression.PartyMembers.Single(item => item.PartyMemberId == bulbasaurId).SimulationScope);
        CollectionAssert.Contains(progression.PartyMembers.Single(item => item.PartyMemberId == pikachuId).Moves.Select(item => item.MoveName).ToList(), "Thunderbolt");

        var recalculateResponse = await client.PostAsync($"/api/routes/{route.Id}:recalculate", content: null);
        recalculateResponse.EnsureSuccessStatusCode();

        var verifyResponse = await client.PostAsync($"/api/routes/{route.Id}:verify", content: null);
        verifyResponse.EnsureSuccessStatusCode();
        var verification = await verifyResponse.Content.ReadFromJsonAsync<RouteVerificationResult>();
        Assert.IsNotNull(verification);
        Assert.IsEmpty(verification.Issues);

        var presetResponse = await client.PostAsJsonAsync("/api/presets", new
        {
            runId = run.Id,
            ivRanges = new[]
            {
                new { stat = "attack", minimum = 20, maximum = 31 },
                new { stat = "speed", minimum = 15, maximum = 31 }
            },
            evPatterns = new[]
            {
                new
                {
                    patternKey = "story-default",
                    effortValues = new { hp = 0, attack = 36, defense = 0, specialAttack = 0, specialDefense = 0, speed = 12 },
                    notes = "mid-game split"
                }
            },
            naturePatterns = new[]
            {
                new { patternKey = "timid", nature = "Timid", notes = "speed focus" }
            },
            notes = "Story preset"
        });
        presetResponse.EnsureSuccessStatusCode();
        var preset = await presetResponse.Content.ReadFromJsonAsync<CalculationPreset>();
        Assert.IsNotNull(preset);
        Assert.AreEqual(1, preset.Revision);

        var presetList = await client.GetFromJsonAsync<List<CalculationPreset>>($"/api/presets?runId={run.Id}");
        Assert.IsNotNull(presetList);
        Assert.AreEqual(preset.Id, presetList.Single().Id);

        var damageResponse = await client.PostAsJsonAsync("/api/calculations/damage", new
        {
            runId = run.Id,
            routeId = route.Id,
            battleId = battle.Id,
            playerPartyMemberId = pikachuId,
            presetId = preset.Id,
            moveName = "Spark",
            movePower = 65,
            moveType = "Electric",
            attacker = new { species = "Pikachu", level = 18, attack = 41, defense = 26, primaryType = "Electric", secondaryType = (string?)null, heldItem = "Magnet" },
            defender = new { species = "Wingull", level = 14, attack = 20, defense = 18, primaryType = "Water", secondaryType = "Flying", heldItem = (string?)null },
            isCritical = true,
            typeEffectivenessOverride = (decimal?)null,
            additionalModifiers = new[]
            {
                new { code = "item", label = "Magnet", multiplier = 1.1m, sourceCode = "item.magnet", sourceLabel = "Magnet", sourceType = "item" }
            }
        });
        damageResponse.EnsureSuccessStatusCode();
        var damage = await damageResponse.Content.ReadFromJsonAsync<DamageCalculationResult>();
        Assert.IsNotNull(damage);
        Assert.IsLessThanOrEqualTo(damage.MaximumDamage, damage.MinimumDamage);
        Assert.AreEqual(4m, damage.AppliedModifiers.Single(item => item.Code == "effectiveness").Multiplier);
        Assert.IsTrue(damage.SourceReferences.Any(item => item.ReferenceType == "calculation-preset"));

        var compareResponse = await client.PostAsJsonAsync("/api/calculations/damage:compare-patterns", new
        {
            baseCase = new
            {
                runId = run.Id,
                routeId = route.Id,
                battleId = battle.Id,
                playerPartyMemberId = pikachuId,
                presetId = preset.Id,
                moveName = "Spark",
                movePower = 65,
                moveType = "Electric",
                attacker = new { species = "Pikachu", level = 18, attack = 41, defense = 26, primaryType = "Electric", secondaryType = (string?)null, heldItem = "Magnet" },
                defender = new { species = "Wingull", level = 14, attack = 20, defense = 18, primaryType = "Water", secondaryType = "Flying", heldItem = (string?)null },
                isCritical = false,
                typeEffectivenessOverride = (decimal?)null,
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
        Assert.HasCount(1, compareResults);

        var thresholdResponse = await client.PostAsJsonAsync("/api/calculations/damage:search-thresholds", new
        {
            runId = run.Id,
            routeId = route.Id,
            battleId = battle.Id,
            playerPartyMemberId = pikachuId,
            presetId = preset.Id,
            moveName = "Spark",
            movePower = 65,
            moveType = "Electric",
            searchStats = new[] { "attack" },
            conditionMode = "allOf",
            conditions = new[]
            {
                new { conditionKey = "min-damage", conditionType = "minimum-damage-at-least", expectedValue = 20 }
            },
            priorityOrder = Array.Empty<string>()
        });
        thresholdResponse.EnsureSuccessStatusCode();
        var threshold = await thresholdResponse.Content.ReadFromJsonAsync<ThresholdSearchResult>();
        Assert.IsNotNull(threshold);
        Assert.AreEqual("solved", threshold.Status);
        Assert.IsNotNull(threshold.BestSolution);
        Assert.IsNotEmpty(threshold.AllMinimalSolutions);
        Assert.IsTrue(threshold.SourceReferences.Any(item => item.ReferenceType == "calculation-preset"));
    }

    [TestMethod]
    public async Task ShareAndImportEndpoints_WorkWithRevisionAndAdminBoundaries()
    {
        using var client = CreateClient("share-user");
        var (run, route, battle, partyMemberId) = await CreateRunWithBattleAsync(client);

        var shareResponse = await client.PostAsJsonAsync("/api/shares/snapshots", new
        {
            runId = run.Id,
            sourceType = "route-plan",
            sourceId = route.Id,
            visibility = "public",
            allowedRoles = new[] { "viewer", "commenter", "reviser" },
            summary = "foundation publish",
            frozenInput = new { routeId = route.Id, source = "route-plan" },
            frozenOutput = new { progressionFingerprint = route.ProgressionFingerprint }
        });
        shareResponse.EnsureSuccessStatusCode();
        var share = await shareResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(share);
        CollectionAssert.Contains(share.AllowedRoles.ToList(), "commenter");
        Assert.IsFalse(string.IsNullOrWhiteSpace(share.ProgressionFingerprint));
        Assert.IsFalse(string.IsNullOrWhiteSpace(share.CalculationInputDigest));
        Assert.IsFalse(string.IsNullOrWhiteSpace(share.CalculationOutputDigest));

        using var anonymousClient = _factory.CreateClient();
        var anonymousReadResponse = await anonymousClient.GetAsync($"/api/shares/{share.Id}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousReadResponse.StatusCode);

        using var viewerClient = CreateClient("viewer-user", "viewer");
        var viewerReadResponse = await viewerClient.GetAsync($"/api/shares/{share.Id}");
        Assert.AreEqual(HttpStatusCode.OK, viewerReadResponse.StatusCode);
        var viewerShare = await viewerReadResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(viewerShare);

        using var commenterClient = CreateClient("commenter-user", "commenter");
        var commenterReadResponse = await commenterClient.GetAsync($"/api/shares/{share.Id}");
        Assert.AreEqual(HttpStatusCode.OK, commenterReadResponse.StatusCode);

        using var memberClient = CreateClient("member-user");
        var forbiddenCommentResponse = await memberClient.PostAsJsonAsync($"/api/shares/{share.Id}/comments", new { revisionId = share.CurrentRevisionId, body = "Intruding comment" });
        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenCommentResponse.StatusCode);

        var otherUserCommentResponse = await commenterClient.PostAsJsonAsync($"/api/shares/{share.Id}/comments", new { revisionId = share.CurrentRevisionId, body = "Allowed comment" });
        otherUserCommentResponse.EnsureSuccessStatusCode();

        var commentedShare = await otherUserCommentResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(commentedShare);
        Assert.HasCount(1, commentedShare.Comments);

        var eventResponse = await client.PostAsJsonAsync($"/api/routes/{route.Id}/events", new
        {
            eventType = "rare-candy",
            summary = "Use Rare Candy",
            linkedBattleId = (Guid?)null,
            moneyDelta = 0,
            sourceReference = "item",
            partyDeltas = new object[]
            {
                new
                {
                    partyMemberId,
                    experienceDelta = 0,
                    effortValueDelta = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    ppDeltas = new[] { new { moveName = "Ember", delta = -30 } },
                    rareCandyLevels = 1,
                    speciesOverride = "Combusken",
                    abilityOverride = (string?)null,
                    natureOverride = (string?)null,
                    heldItemOverride = (string?)null,
                    replaceMoves = (object[]?)null,
                    notes = "force warning"
                }
            }
        });
        eventResponse.EnsureSuccessStatusCode();

        var progression = await client.GetFromJsonAsync<RouteProgressionProjection>($"/api/routes/{route.Id}/progression");
        Assert.IsNotNull(progression);
        Assert.IsNotEmpty(progression.Warnings.Where(item => item.Code == "pp.negative"));

        var damageResponse = await client.PostAsJsonAsync("/api/calculations/damage", new
        {
            runId = run.Id,
            routeId = route.Id,
            battleId = battle.Id,
            playerPartyMemberId = partyMemberId,
            moveName = "Ember",
            movePower = 40,
            moveType = "Fire",
            attacker = new { species = "Torchic", level = 15, attack = 36, defense = 24, primaryType = "Fire", secondaryType = (string?)null, heldItem = "Charcoal" },
            defender = new { species = "Poochyena", level = 9, attack = 17, defense = 15, primaryType = "Dark", secondaryType = (string?)null, heldItem = (string?)null },
            isCritical = false,
            typeEffectivenessOverride = (decimal?)null,
            additionalModifiers = Array.Empty<object>()
        });
        damageResponse.EnsureSuccessStatusCode();
        var damage = await damageResponse.Content.ReadFromJsonAsync<DamageCalculationResult>();
        Assert.IsNotNull(damage);
        Assert.IsNotEmpty(damage.Warnings);

        var publishRevisionResponse = await memberClient.PostAsJsonAsync($"/api/shares/{share.Id}:revise", new { summary = "after rare candy" });
        Assert.AreEqual(HttpStatusCode.Forbidden, publishRevisionResponse.StatusCode);

        using var reviserClient = CreateClient("reviser-user", "reviser");
        publishRevisionResponse = await reviserClient.PostAsJsonAsync($"/api/shares/{share.Id}:revise", new { summary = "after rare candy" });
        publishRevisionResponse.EnsureSuccessStatusCode();
        var revisedShare = await publishRevisionResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(revisedShare);
        Assert.HasCount(2, revisedShare.Revisions);

        var diffResponse = await viewerClient.PostAsync($"/api/shares/{share.Id}:diff?baseRevisionId={revisedShare.BaseRevisionId}&targetRevisionId={revisedShare.CurrentRevisionId}", content: null);
        diffResponse.EnsureSuccessStatusCode();
        var diff = await diffResponse.Content.ReadFromJsonAsync<ShareDiffResult>();
        Assert.IsNotNull(diff);
        Assert.IsTrue(diff.ChangedFields.Count > 0 || diff.ChangedVersions.Count > 0 || diff.ChangedPolicies.Count >= 0);

        var revisionsResponse = await reviserClient.GetAsync($"/api/shares/{share.Id}/revisions");
        revisionsResponse.EnsureSuccessStatusCode();
        var revisions = await revisionsResponse.Content.ReadFromJsonAsync<List<RouteRevision>>();
        Assert.IsNotNull(revisions);
        var snapshotDocument = System.Text.Json.JsonSerializer.Deserialize<RouteSnapshotDocument>(
            revisions.Single(item => item.Id == share.CurrentRevisionId).SnapshotJson,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.IsNotNull(snapshotDocument);
        Assert.AreEqual(share.ProgressionFingerprint, snapshotDocument.ProgressionFingerprint);
        Assert.AreEqual(share.CalculationInputDigest, snapshotDocument.CalculationInputDigest);
        Assert.AreEqual(share.CalculationOutputDigest, snapshotDocument.CalculationOutputDigest);

        var calculationShareResponse = await client.PostAsJsonAsync("/api/shares/snapshots", new
        {
            runId = run.Id,
            sourceType = "calculation",
            sourceId = battle.Id,
            visibility = "public",
            allowedRoles = new[] { "viewer", "commenter", "reviser" },
            summary = "calculation share",
            frozenInput = new { battleId = battle.Id, source = "calculation" },
            frozenOutput = new { result = "damage" }
        });
        calculationShareResponse.EnsureSuccessStatusCode();

        var verificationShareResponse = await client.PostAsJsonAsync("/api/shares/snapshots", new
        {
            runId = run.Id,
            sourceType = "verification",
            sourceId = route.Id,
            visibility = "public",
            allowedRoles = new[] { "viewer", "commenter", "reviser" },
            summary = "verification share",
            frozenInput = new { routeId = route.Id, source = "verification" },
            frozenOutput = new { result = "route-verification" }
        });
        verificationShareResponse.EnsureSuccessStatusCode();

        var deterministicShareResponse = await client.PostAsJsonAsync("/api/shares/snapshots", new
        {
            runId = run.Id,
            sourceType = "route-plan",
            sourceId = route.Id,
            visibility = "public",
            allowedRoles = new[] { "viewer", "commenter", "reviser" },
            summary = "foundation publish",
            frozenInput = new { routeId = route.Id, source = "route-plan" },
            frozenOutput = new { progressionFingerprint = route.ProgressionFingerprint }
        });
        deterministicShareResponse.EnsureSuccessStatusCode();
        var deterministicShare = await deterministicShareResponse.Content.ReadFromJsonAsync<SharedRouteSnapshot>();
        Assert.IsNotNull(deterministicShare);
        Assert.AreEqual(revisedShare.SharePolicyDigest, deterministicShare.SharePolicyDigest);
        Assert.AreEqual(revisedShare.CalculationInputDigest, deterministicShare.CalculationInputDigest);
        Assert.AreEqual(revisedShare.CalculationOutputDigest, deterministicShare.CalculationOutputDigest);
        CollectionAssert.AreEqual(revisedShare.FixedVersionCatalog.ImportJobIds.ToArray(), deterministicShare.FixedVersionCatalog.ImportJobIds.ToArray());

        var forbiddenResponse = await memberClient.PostAsJsonAsync("/api/admin/import-jobs:dry-run", new { rulesetId = SeedRulesetId, workbookName = "emerald.xlsx" });
        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var adminClient = CreateClient("admin-user", "Administrator");
        var importResponse = await adminClient.PostAsJsonAsync("/api/admin/imports", new { rulesetId = SeedRulesetId, workbookName = "story-route.xlsm", mode = "commit", sourceType = "spreadsheet", workbookContent = LoadWorkbookFixtureBase64() });
        importResponse.EnsureSuccessStatusCode();
        var job = await importResponse.Content.ReadFromJsonAsync<ImportJob>();
        Assert.IsNotNull(job);
        Assert.AreEqual("commit", job.Mode);
        Assert.AreEqual("spreadsheet", job.SourceType);
        Assert.IsNotEmpty(job.RowResults);
        Assert.IsNotEmpty(job.AuditTrail);

        var jobLookup = await adminClient.GetFromJsonAsync<ImportJob>($"/api/admin/imports/{job.Id}");
        Assert.IsNotNull(jobLookup);
        Assert.IsGreaterThan(0, jobLookup.DuplicateSummary.TotalRows);

        var masterVersionSets = await adminClient.GetFromJsonAsync<List<MasterVersionSet>>("/api/admin/master-version-sets");
        Assert.IsNotNull(masterVersionSets);
        Assert.IsGreaterThanOrEqualTo(2, masterVersionSets.Count);
        Assert.IsTrue(masterVersionSets.Any(item => item.VersionCatalog.ImportJobIds.Contains(job.Id)));
    }

    private HttpClient CreateClient(string userId, string role = "Member")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task<(RunAggregate Run, RoutePlan Route, BattleDefinition Battle, Guid PartyMemberId)> CreateRunWithBattleAsync(HttpClient client)
    {
        var runResponse = await client.PostAsJsonAsync("/api/runs", new { rulesetId = SeedRulesetId, name = "Share run" });
        runResponse.EnsureSuccessStatusCode();
        var run = await runResponse.Content.ReadFromJsonAsync<RunAggregate>() ?? throw new InvalidOperationException();

        var partyMemberId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var initialStateResponse = await client.PutAsJsonAsync($"/api/runs/{run.Id}/initial-state", new
        {
            baselineMoney = 2100,
            baselineParty = new object[]
            {
                new
                {
                    partyMemberId,
                    slot = 1,
                    species = "Torchic",
                    level = 15,
                    experience = 3375,
                    individualValues = new { hp = 31, attack = 31, defense = 31, specialAttack = 31, specialDefense = 31, speed = 31 },
                    effortValues = new { hp = 0, attack = 0, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    nature = "Adamant",
                    ability = "Blaze",
                    combatStats = new { hp = 44, attack = 36, defense = 24, specialAttack = 31, specialDefense = 26, speed = 29 },
                    typing = new { primaryType = "Fire", secondaryType = (string?)null },
                    heldItem = "Charcoal",
                    moves = new[] { new { moveName = "Ember", maxPp = 25, currentPp = 10 } },
                    memo = "share fixture",
                    isBattleSimulatorEnabled = true
                }
            },
            memo = "share fixture"
        });
        initialStateResponse.EnsureSuccessStatusCode();
        run = await initialStateResponse.Content.ReadFromJsonAsync<RunAggregate>() ?? throw new InvalidOperationException();

        var routeResponse = await client.PostAsJsonAsync($"/api/runs/{run.Id}/routes", new { name = "Route 102", simulatedPartyMemberIds = new[] { partyMemberId } });
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
                new
                {
                    species = "Poochyena",
                    level = 9,
                    hp = 26,
                    attack = 17,
                    defense = 15,
                    primaryType = "Dark",
                    secondaryType = (string?)null,
                    baseExperienceYield = 55,
                    effortValueYield = new { hp = 0, attack = 1, defense = 0, specialAttack = 0, specialDefense = 0, speed = 0 },
                    note = "fixture"
                }
            },
            suggestedPartyMemberIds = new[] { partyMemberId },
            participations = new object[]
            {
                new
                {
                    partyMemberId,
                    participationMode = "active",
                    shareRatio = 1.0m,
                    suggestedRole = "lead",
                    outcomeChecklist = new { sentOutAndDefeated = true, defeatedWhileInReserve = false, didNotDefeat = false, intentionalLoss = false }
                }
            },
            notes = "fixture"
        });
        battleResponse.EnsureSuccessStatusCode();
        var battle = await battleResponse.Content.ReadFromJsonAsync<BattleDefinition>() ?? throw new InvalidOperationException();

        return (run, route, battle, partyMemberId);
    }

    private static string LoadWorkbookFixtureBase64()
    {
        var workbookPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../参考/Re6 オメガルビー(ヌマクロー、ラティオス、グラードン).xlsm のコピー.xlsm"));
        return Convert.ToBase64String(File.ReadAllBytes(workbookPath));
    }
}
