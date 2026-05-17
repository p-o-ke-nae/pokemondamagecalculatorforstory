using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Models;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Repositories;

namespace PokemonDamageCalculatorForStory.Infrastructure.Seed;

/// <summary>初期 master data を投入します。</summary>
public static class AppDbSeeder
{
    /// <summary>必要な初期データを投入します。</summary>
    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Rulesets.AnyAsync(cancellationToken))
        {
            return;
        }

        var rulesetRepository = new RulesetRepository(context);
        var ruleset = new Ruleset(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "gen3-emerald-story",
            "Gen3",
            "Pokemon Emerald",
            "story-v1",
            "active",
            "Hoenn story progression 向けの攻略 ruleset");
        await rulesetRepository.SeedRulesetAsync(ruleset, cancellationToken);

        await rulesetRepository.SaveMasterVersionSetAsync(
            new MasterVersionSet(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ruleset.Id,
                "emerald-master-2026-05",
                DateTimeOffset.Parse("2026-05-16T00:00:00Z"),
                true,
                new VersionCatalog(
                    "gen3-emerald-story:damage:story-v1",
                    "gen3-emerald-story:experience:story-v1",
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
                new[]
                {
                    new SourceReference("master.trainers", "trainers.csv", "spreadsheet"),
                    new SourceReference("master.moves", "moves.csv", "spreadsheet")
                }),
            cancellationToken);
    }
}
