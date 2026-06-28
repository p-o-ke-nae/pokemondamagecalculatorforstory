using PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

/// <summary>
/// 戦闘で使用される技を表す。
/// </summary>
public class Move
{
    private readonly IMoveDamageEffect[] _damageEffects;
    private readonly IMoveBattleEffect[] _battleEffects;

    /// <summary>
    /// 技を初期化する。
    /// </summary>
    /// <param name="name">技名。</param>
    /// <param name="type">技タイプ。</param>
    /// <param name="category">技分類。</param>
    /// <param name="power">威力。</param>
    /// <param name="targetScope">対象範囲。</param>
    /// <param name="damageEffects">ダメージ計算効果。</param>
    /// <param name="battleEffects">戦闘状態効果。</param>
    public Move(
        string name,
        PokemonType type,
        MoveCategory category,
        Power power,
        TargetScope targetScope,
        IMoveDamageEffect[] damageEffects,
        IMoveBattleEffect[] battleEffects)
    {
        Name = name;
        Type = type;
        Category = category;
        Power = power;
        TargetScope = targetScope;
        _damageEffects = damageEffects.ToArray();
        _battleEffects = battleEffects.ToArray();
    }

    /// <summary>技名。</summary>
    public string Name { get; }

    /// <summary>技タイプ。</summary>
    public PokemonType Type { get; }

    /// <summary>技分類。</summary>
    public MoveCategory Category { get; }

    /// <summary>威力。</summary>
    public Power Power { get; }

    /// <summary>対象範囲。</summary>
    public TargetScope TargetScope { get; }

    /// <summary>ダメージ計算効果一覧。</summary>
    public IMoveDamageEffect[] DamageEffects => _damageEffects.ToArray();

    /// <summary>戦闘状態効果一覧。</summary>
    public IMoveBattleEffect[] BattleEffects => _battleEffects.ToArray();
}