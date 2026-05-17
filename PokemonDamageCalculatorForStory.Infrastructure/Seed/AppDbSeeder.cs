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
            "Hoenn story progression 向けの簡易 foundation ruleset");
        await rulesetRepository.SeedRulesetAsync(ruleset, cancellationToken);

        await rulesetRepository.SaveMasterVersionSetAsync(
            new MasterVersionSet(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ruleset.Id,
                "emerald-master-2026-05-foundation",
                DateTimeOffset.Parse("2026-05-16T00:00:00Z"),
                true,
                new VersionCatalog(
                    "gen3-emerald-story:damage:story-v1",
                    "gen3-emerald-story:experience:story-v1",
                    "pokemon-master-foundation",
                    "move-master-foundation",
                    "ability-master-foundation",
                    "item-master-foundation",
                    "type-chart-foundation",
                    "nature-master-foundation",
                    "story-enemy-master-foundation",
                    "experience-table-foundation",
                    "effort-value-master-foundation",
                    "pp-rule-foundation",
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
