using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

/// <summary>
/// 戦闘中に参照されるポケモンの状態を表す。
/// </summary>
public class BattlePokemon
{
    /// <summary>
    /// 戦闘用ポケモンを初期化する。
    /// </summary>
    /// <param name="level">レベル。</param>
    /// <param name="typing">タイプ情報。</param>
    /// <param name="stats">能力値。</param>
    /// <param name="statStages">ランク変化。</param>
    /// <param name="ability">特性。</param>
    /// <param name="heldItem">もちもの。</param>
    public BattlePokemon(
        Level level,
        PokemonTyping typing,
        PokemonStats stats,
        StatStages statStages,
        Ability ability,
        Item heldItem)
        : this(level, typing, stats, statStages, ability, heldItem, StatusAilment.None)
    {
    }

    /// <summary>
    /// 戦闘用ポケモンを初期化する。
    /// </summary>
    /// <param name="level">レベル。</param>
    /// <param name="typing">タイプ情報。</param>
    /// <param name="stats">能力値。</param>
    /// <param name="statStages">ランク変化。</param>
    /// <param name="ability">特性。</param>
    /// <param name="heldItem">もちもの。</param>
    /// <param name="statusAilment">状態異常。</param>
    public BattlePokemon(
        Level level,
        PokemonTyping typing,
        PokemonStats stats,
        StatStages statStages,
        Ability ability,
        Item heldItem,
        StatusAilment statusAilment)
    {
        Level = level;
        Typing = typing;
        Stats = stats;
        StatStages = statStages;
        Ability = ability;
        HeldItem = heldItem;
        StatusAilment = statusAilment;
    }

    /// <summary>レベル。</summary>
    public Level Level { get; }

    /// <summary>現在のタイプ情報。</summary>
    public PokemonTyping Typing { get; }

    /// <summary>能力値。</summary>
    public PokemonStats Stats { get; }

    /// <summary>ランク変化。</summary>
    public StatStages StatStages { get; }

    /// <summary>特性。</summary>
    public Ability Ability { get; }

    /// <summary>もちもの。</summary>
    public Item HeldItem { get; }

    /// <summary>状態異常。</summary>
    public StatusAilment StatusAilment { get; private set; }

    /// <summary>
    /// 主要状態異常を設定する。
    /// </summary>
    /// <param name="primaryAilment">設定する主要状態異常。</param>
    public void SetPrimaryStatusAilment(PrimaryStatusAilment primaryAilment)
    {
        StatusAilment = StatusAilment.WithPrimaryAilment(primaryAilment);
    }

    /// <summary>
    /// 主要状態異常を解除する。
    /// </summary>
    public void ClearPrimaryStatusAilment()
    {
        StatusAilment = StatusAilment.ClearPrimaryAilment();
    }

    /// <summary>
    /// 追加状態を付与する。
    /// </summary>
    /// <param name="condition">付与する追加状態。</param>
    public void AddAdditionalCondition(AdditionalBattleCondition condition)
    {
        StatusAilment = StatusAilment.AddAdditionalCondition(condition);
    }

    /// <summary>
    /// 追加状態を解除する。
    /// </summary>
    /// <param name="condition">解除する追加状態。</param>
    public void RemoveAdditionalCondition(AdditionalBattleCondition condition)
    {
        StatusAilment = StatusAilment.RemoveAdditionalCondition(condition);
    }
}