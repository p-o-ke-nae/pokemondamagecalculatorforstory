using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation;

/// <summary>
/// ダメージ計算式の入力仕様を表す。
/// </summary>
public class DamageSpec
{
    /// <summary>
    /// ダメージ計算仕様を初期化する。
    /// 攻撃値の参照元は攻撃側ポケモンとする。
    /// </summary>
    /// <param name="attackSource">攻撃参照元。</param>
    /// <param name="defenseSource">防御参照元。</param>
    /// <param name="powerOverride">威力上書き値。</param>
    /// <param name="fixedDamage">固定ダメージ値。</param>
    /// <param name="modifiers">補正一覧。</param>
    public DamageSpec(
        StatSelector attackSource,
        StatSelector defenseSource,
        Power powerOverride,
        int fixedDamage)
        : this(attackSource, DamageStatOwner.Attacker, defenseSource, powerOverride, fixedDamage)
    {
    }

    /// <summary>
    /// ダメージ計算仕様を初期化する。
    /// </summary>
    /// <param name="attackSource">攻撃参照元。</param>
    /// <param name="attackStatOwner">攻撃値を参照するポケモン。</param>
    /// <param name="defenseSource">防御参照元。</param>
    /// <param name="powerOverride">威力上書き値。</param>
    /// <param name="fixedDamage">固定ダメージ値。</param>
    public DamageSpec(
        StatSelector attackSource,
        DamageStatOwner attackStatOwner,
        StatSelector defenseSource,
        Power powerOverride,
        int fixedDamage)
    {
        AttackSource = attackSource;
        AttackStatOwner = attackStatOwner;
        DefenseSource = defenseSource;
        PowerOverride = powerOverride;
        FixedDamage = fixedDamage;
    }

    /// <summary>攻撃参照元。</summary>
    public StatSelector AttackSource { get; }

    /// <summary>攻撃値を参照するポケモン。</summary>
    public DamageStatOwner AttackStatOwner { get; }

    /// <summary>防御参照元。</summary>
    public StatSelector DefenseSource { get; }

    /// <summary>威力上書き値。</summary>
    public Power PowerOverride { get; }

    /// <summary>固定ダメージ値。</summary>
    public int FixedDamage { get; }
}