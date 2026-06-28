using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;

/// <summary>
/// 対象のタイプを書き換える戦闘効果を表す。
/// </summary>
public class TypeChangeEffect : IMoveBattleEffect
{
    private readonly PokemonType[] _newTypes;

    /// <summary>
    /// タイプ変更効果を初期化する。
    /// </summary>
    /// <param name="newTypes">置き換え後のタイプ配列。</param>
    public TypeChangeEffect(PokemonType[] newTypes)
    {
        _newTypes = newTypes.ToArray();
    }

    /// <summary>置き換え後のタイプ配列。</summary>
    public PokemonType[] NewTypes => _newTypes.ToArray();

    /// <summary>
    /// 対象のタイプを新しいタイプ配列に置き換える。
    /// </summary>
    /// <param name="target">効果対象。</param>
    /// <param name="context">戦闘文脈。</param>
    /// <inheritdoc />
    public void Apply(BattlePokemon target, DamageContext context)
    {
        target.Typing.ReplaceWith(_newTypes);
    }
}