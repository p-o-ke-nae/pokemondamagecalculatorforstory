using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Modifiers;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;

internal static class AttackStatModifierEvaluator
{
    public static AttackStatModifier Evaluate(DamageSpec spec, DamageContext context)
    {
        var subject = context.GetAttackStatPokemon(spec.AttackStatOwner);
        var modifier = AttackStatModifier.None;

        foreach (var effect in subject.Ability.AttackStatEffects)
        {
            modifier = modifier.Compose(
                effect.GetAttackStatModifier(subject, spec, context));
        }

        return modifier;
    }
}