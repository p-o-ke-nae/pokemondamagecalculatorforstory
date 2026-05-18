using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Mappers;

public static class PersistedRuleSetMapper
{
    public static RuleSet ToDomainEntity(this PersistedRuleSet persisted)
        => RuleSet.Restore(persisted.Id, persisted.Slug, persisted.Generation, persisted.Title, persisted.Version, persisted.Status, persisted.Summary);

    public static PersistedRuleSet ToPersistedModel(this RuleSet entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Generation = entity.Generation,
            Title = entity.Title,
            Version = entity.Version,
            Status = entity.Status,
            Summary = entity.Summary
        };
}
