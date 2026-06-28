using PokemonDamageCalculatorForStory.Domain.DamageCalculation;
using PokemonDamageCalculatorForStory.Domain.DamageCalculation.MoveEffects;
using System.Diagnostics;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

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

    /// <summary>
    /// ダメージ計算用の DamageSpec を作成する。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="UnreachableException"></exception>
    public DamageSpec CreateBaseDamageSpec()
    {
        return Category switch
        {
            MoveCategory.Physical => new DamageSpec(
                StatSelector.Attack,
                StatSelector.Defense,
                Power,
                fixedDamage: 0),

            MoveCategory.Special => new DamageSpec(
                StatSelector.SpecialAttack,
                StatSelector.SpecialDefense,
                Power,
                fixedDamage: 0),

            MoveCategory.Status => throw new InvalidOperationException(
                $"変化技 {Name} はダメージ計算用の DamageSpec を持ちません。"),

            _ => throw new UnreachableException(
                $"未対応の MoveCategory: {Category}")
        };
    }
}