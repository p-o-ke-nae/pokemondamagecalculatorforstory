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
        if (string.IsNullOrWhiteSpace(ownerUserId)) throw new InvalidOperationException("Owner user id is required.");
        if (ruleSetId == Guid.Empty) throw new InvalidOperationException("RuleSetId is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name is required.");
        if (string.IsNullOrWhiteSpace(status)) throw new InvalidOperationException("Status is required.");

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
        var entity = Create(ownerUserId, ruleSetId, name, status);
        entity.Id = id;
        return entity;
    }
}
