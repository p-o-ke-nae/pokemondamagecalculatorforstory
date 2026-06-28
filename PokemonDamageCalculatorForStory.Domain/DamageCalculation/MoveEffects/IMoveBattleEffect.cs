using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 技による戦闘状態を変更する効果を表す。
/// </summary>
public interface IMoveBattleEffect
{
    /// <summary>
    /// 対象ポケモンへ戦闘効果を適用する。
    /// </summary>
    /// <param name="target">効果対象。</param>
    /// <param name="context">戦闘文脈。</param>
    void Apply(BattlePokemon target, DamageContext context);
}