using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation;

/// <summary>
/// ダメージ計算時に共有される戦闘文脈を表す。
/// </summary>
public class DamageContext
{
    /// <summary>
    /// ダメージ計算文脈を初期化する。
    /// </summary>
    /// <param name="battle">戦闘情報。</param>
    /// <param name="attacker">攻撃側ポケモン。</param>
    /// <param name="defender">防御側ポケモン。</param>
    /// <param name="moveCategory">技分類。</param>
    /// <param name="moveTargetCount">対象数。</param>
    public DamageContext(BattlePokemon attacker, BattlePokemon defender, MoveCategory moveCategory, MoveTargetCount moveTargetCount)
    {
        Attacker = attacker;
        Defender = defender;
        MoveCategory = moveCategory;
        MoveTargetCount = moveTargetCount;
    }

    /// <summary>攻撃側ポケモン。</summary>
    public BattlePokemon Attacker { get; }

    /// <summary>防御側ポケモン。</summary>
    public BattlePokemon Defender { get; }

    /// <summary>対象数。</summary>
    public MoveTargetCount MoveTargetCount { get; }

    /// <summary>技分類。</summary>
    public MoveCategory MoveCategory { get; }

    /// <summary>
    /// 指定された攻撃値を参照するポケモンを取得する。
    /// </summary>
    /// <param name="owner">攻撃値の参照元。</param>
    /// <returns>攻撃値を参照するポケモン。</returns>
    public BattlePokemon GetAttackStatPokemon(DamageStatOwner owner)
    {
        return owner == DamageStatOwner.Attacker ? Attacker : Defender;
    }
}