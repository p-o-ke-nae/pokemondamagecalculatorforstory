using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class CalculationResult
{
    public Guid Id { get; private set; }
    public Guid RunId { get; private set; }
    public Guid BattleId { get; private set; }
    public string AttackerParams { get; private set; } = string.Empty;
    public string DefenderParams { get; private set; } = string.Empty;
    public IReadOnlyList<int> DamageRolls { get; private set; } = Array.Empty<int>();

    private CalculationResult()
    {
    }

    public static CalculationResult Create(
        Guid runId,
        Guid battleId,
        string attackerParams,
        string defenderParams,
        IReadOnlyList<int> damageRolls)
    {
        if (runId == Guid.Empty) throw new ValidationException("RunId is required.");
        if (battleId == Guid.Empty) throw new ValidationException("BattleId is required.");
        if (string.IsNullOrWhiteSpace(attackerParams)) throw new ValidationException("AttackerParams is required.");
        if (string.IsNullOrWhiteSpace(defenderParams)) throw new ValidationException("DefenderParams is required.");
        if (damageRolls.Count == 0) throw new ValidationException("DamageRolls is required.");

        return new CalculationResult
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            BattleId = battleId,
            AttackerParams = attackerParams.Trim(),
            DefenderParams = defenderParams.Trim(),
            DamageRolls = damageRolls.ToArray()
        };
    }

    public static CalculationResult Restore(
        Guid id,
        Guid runId,
        Guid battleId,
        string attackerParams,
        string defenderParams,
        IReadOnlyList<int> damageRolls)
    {
        return new CalculationResult
        {
            Id = id,
            RunId = runId,
            BattleId = battleId,
            AttackerParams = attackerParams.Trim(),
            DefenderParams = defenderParams.Trim(),
            DamageRolls = damageRolls.ToArray()
        };
    }
}
