using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Mappers;

public static class PersistedRunMapper
{
    public static Run ToDomainEntity(this PersistedRun persisted)
        => Run.Restore(persisted.Id, persisted.OwnerUserId, persisted.RuleSetId, persisted.Name, persisted.Status);

    public static PersistedRun ToPersistedModel(this Run entity)
        => new()
        {
            Id = entity.Id,
            OwnerUserId = entity.OwnerUserId,
            RuleSetId = entity.RuleSetId,
            Name = entity.Name,
            Status = entity.Status
        };
}
