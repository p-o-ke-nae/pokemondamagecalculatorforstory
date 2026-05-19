using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Calculations;

public static class PokemonDamageFormula
{
    // Current default formula used by the supported rule set.
    // Add generation-specific formulas beside this type when rules diverge.
    public static IReadOnlyList<int> CalculateRolls(
        PokemonLevel attackerLevel,
        AttackStat attackStat,
        MovePower movePower,
        bool hasStab,
        DefenseStat defenseStat,
        TypeEffectivenessMultiplier typeEffectiveness)
    {
        var levelFactor = (2 * attackerLevel.Value / 5) + 2;
        var baseDamage = (int)Math.Floor((double)(levelFactor * movePower.Value * attackStat.Value) / defenseStat.Value / 50) + 2;

        return Enumerable.Range(85, 16)
            .Select(roll =>
            {
                var rolledDamage = (int)Math.Floor(baseDamage * roll / 100.0);
                var stabbedDamage = (int)Math.Floor(rolledDamage * (hasStab ? 1.5 : 1.0));
                return (int)Math.Floor(stabbedDamage * typeEffectiveness.Value);
            })
            .ToArray();
    }
}
