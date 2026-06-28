using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.Policies;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

/// <summary>
/// 戦闘全体の文脈を表し、ダメージ計算と戦闘効果の適用を仲介する。
/// </summary>
public class Battle
{
    /// <summary>
    /// 戦闘インスタンスを初期化する。
    /// </summary>
    /// <param name="format">対戦形式。</param>
    /// <param name="field">フィールド状態。</param>
    /// <param name="damagePolicy">世代別のダメージ計算ポリシー。</param>
    public Battle(BattleFormat format, Field field, IPokemonDamagePolicy damagePolicy)
    {
        Format = format;
        Field = field;
        DamagePolicy = damagePolicy;
    }

    /// <summary>対戦形式。</summary>
    public BattleFormat Format { get; }

    /// <summary>フィールド状態。</summary>
    public Field Field { get; }

    /// <summary>ダメージ計算ポリシー。</summary>
    public IPokemonDamagePolicy DamagePolicy { get; }

    /// <summary>
    /// 指定した攻撃側、防御側、技に基づくダメージ結果を計算する。
    /// </summary>
    /// <param name="attacker">攻撃側ポケモン。</param>
    /// <param name="defender">防御側ポケモン。</param>
    /// <param name="move">使用する技。</param>
    /// <returns>ダメージ結果。</returns>
    public DamageResult CalculateDamage(BattlePokemon attacker, BattlePokemon defender, Move move)
    {
        var context = new DamageContext(attacker, defender, move.Category, new MoveTargetCount(1));
        var spec = move.CreateBaseDamageSpec();

        foreach (var effect in move.DamageEffects)
        {
            spec = effect.Apply(spec, context);
        }

        return DamagePolicy.Calculate(spec, context);
    }

    /// <summary>
    /// 技に紐づく戦闘効果を対象へ適用する。
    /// </summary>
    /// <param name="attacker">攻撃側ポケモン。</param>
    /// <param name="defender">防御側ポケモン。</param>
    /// <param name="move">使用する技。</param>
    public void ApplyBattleEffects(BattlePokemon attacker, BattlePokemon defender, Move move)
    {
        var context = new DamageContext(attacker, defender, move.Category, new MoveTargetCount(1));

        foreach (var effect in move.BattleEffects)
        {
            effect.Apply(defender, context);
        }
    }
}