using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class Run
{
    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public Guid RuleSetId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;

    private Run()
    {
    }

    public static Run Create(string ownerUserId, Guid ruleSetId, string name, string status)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId)) throw new ValidationException("Owner user id is required.");
        if (ruleSetId == Guid.Empty) throw new ValidationException("RuleSetId is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name is required.");
        if (string.IsNullOrWhiteSpace(status)) throw new ValidationException("Status is required.");

        return new Run
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId.Trim(),
            RuleSetId = ruleSetId,
            Name = name.Trim(),
            Status = status.Trim()
        };
    }

    public static Run Restore(Guid id, string ownerUserId, Guid ruleSetId, string name, string status)
    {
        return new Run
        {
            Id = id,
            OwnerUserId = ownerUserId.Trim(),
            RuleSetId = ruleSetId,
            Name = name.Trim(),
            Status = status.Trim()
        };
    }
}
