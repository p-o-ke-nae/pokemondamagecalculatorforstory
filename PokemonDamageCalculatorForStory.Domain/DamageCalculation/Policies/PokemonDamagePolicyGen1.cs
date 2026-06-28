namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;

/// <summary>
/// 第1世代向けのダメージ計算ポリシーを表す。
/// </summary>
public class PokemonDamagePolicyGen1 : IPokemonDamagePolicy
{
    /// <summary>
    /// 第1世代ルールに基づくダメージ結果を算出する。
    /// </summary>
    /// <param name="spec">ダメージ計算仕様。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <returns>ダメージ結果。</returns>
    /// <inheritdoc />
    public DamageResult Calculate(DamageSpec spec, DamageContext context)
    {
        if (spec.FixedDamage > 0)
        {
            return new DamageResult([spec.FixedDamage]);
        }

        var burnCorrection = BurnAttackCorrectionEvaluator.Evaluate(spec, context);
        var attackValue = context.Attacker.Stats.Get(spec.AttackSource);
        var correctedAttackValue = burnCorrection.ApplyTo(attackValue);
        var defenseValue = context.Defender.Stats.Get(spec.DefenseSource);
        var damage = CalculateBaseDamage(context.Attacker.Level.Value, spec.PowerOverride.Value, correctedAttackValue, defenseValue);

        return new DamageResult([damage]);
    }

    private static int CalculateBaseDamage(int level, int power, int attack, int defense)
    {
        var damage = (level * 2 / 5) + 2;
        damage = damage * power * attack / defense;
        damage = damage / 50;
        return damage + 2;
    }
}