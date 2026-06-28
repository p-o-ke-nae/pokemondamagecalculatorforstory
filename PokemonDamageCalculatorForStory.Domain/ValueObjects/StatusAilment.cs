namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// 戦闘中の状態異常を表す。
/// どくやまひのような排他的な主要状態と、混乱ややどりぎのたねのような併用可能状態をまとめて保持する。
/// </summary>
public sealed class StatusAilment
{
    private const AdditionalBattleCondition ValidAdditionalConditions =
        AdditionalBattleCondition.Confusion |
        AdditionalBattleCondition.LeechSeed;

    /// <summary>
    /// 状態異常なしの状態を初期化する。
    /// </summary>
    public StatusAilment()
        : this(PrimaryStatusAilment.None, AdditionalBattleCondition.None)
    {
    }

    /// <summary>
    /// 状態異常を初期化する。
    /// </summary>
    /// <param name="primaryAilment">主要状態異常。</param>
    /// <param name="additionalConditions">併用可能な追加状態。</param>
    public StatusAilment(PrimaryStatusAilment primaryAilment, AdditionalBattleCondition additionalConditions)
    {
        EnsureValidAdditionalConditions(additionalConditions);

        PrimaryAilment = primaryAilment;
        AdditionalConditions = additionalConditions;
    }

    /// <summary>状態異常なしを表すインスタンス。</summary>
    public static StatusAilment None { get; } = new();

    /// <summary>どくやまひのような排他的な主要状態異常。</summary>
    public PrimaryStatusAilment PrimaryAilment { get; }

    /// <summary>混乱ややどりぎのたねのような併用可能な追加状態。</summary>
    public AdditionalBattleCondition AdditionalConditions { get; }

    /// <summary>主要状態異常を持つかどうか。</summary>
    public bool HasPrimaryAilment => PrimaryAilment != PrimaryStatusAilment.None;

    /// <summary>何らかの状態異常を持つかどうか。</summary>
    public bool HasAnyAilment => HasPrimaryAilment || AdditionalConditions != AdditionalBattleCondition.None;

    /// <summary>
    /// 主要状態異常を差し替えた新しい状態異常を返す。
    /// </summary>
    /// <param name="primaryAilment">設定する主要状態異常。</param>
    /// <returns>更新後の状態異常。</returns>
    public StatusAilment WithPrimaryAilment(PrimaryStatusAilment primaryAilment)
    {
        return new StatusAilment(primaryAilment, AdditionalConditions);
    }

    /// <summary>
    /// 主要状態異常を解除した新しい状態異常を返す。
    /// </summary>
    /// <returns>更新後の状態異常。</returns>
    public StatusAilment ClearPrimaryAilment()
    {
        return WithPrimaryAilment(PrimaryStatusAilment.None);
    }

    /// <summary>
    /// 指定した追加状態を付与した新しい状態異常を返す。
    /// </summary>
    /// <param name="condition">付与する追加状態。</param>
    /// <returns>更新後の状態異常。</returns>
    public StatusAilment AddAdditionalCondition(AdditionalBattleCondition condition)
    {
        EnsureConditionIsSpecified(condition);
        EnsureValidAdditionalConditions(condition);

        return new StatusAilment(PrimaryAilment, AdditionalConditions | condition);
    }

    /// <summary>
    /// 指定した追加状態を解除した新しい状態異常を返す。
    /// </summary>
    /// <param name="condition">解除する追加状態。</param>
    /// <returns>更新後の状態異常。</returns>
    public StatusAilment RemoveAdditionalCondition(AdditionalBattleCondition condition)
    {
        EnsureConditionIsSpecified(condition);
        EnsureValidAdditionalConditions(condition);

        return new StatusAilment(PrimaryAilment, AdditionalConditions & ~condition);
    }

    /// <summary>
    /// 指定した追加状態を持つかどうかを返す。
    /// </summary>
    /// <param name="condition">判定する追加状態。</param>
    /// <returns>含む場合は <see langword="true" />。</returns>
    public bool HasAdditionalCondition(AdditionalBattleCondition condition)
    {
        EnsureConditionIsSpecified(condition);
        EnsureValidAdditionalConditions(condition);

        return (AdditionalConditions & condition) == condition;
    }

    private static void EnsureConditionIsSpecified(AdditionalBattleCondition condition)
    {
        if (condition == AdditionalBattleCondition.None)
        {
            throw new ArgumentOutOfRangeException(nameof(condition), condition, "追加状態を指定してください。");
        }
    }

    private static void EnsureValidAdditionalConditions(AdditionalBattleCondition additionalConditions)
    {
        if ((additionalConditions & ~ValidAdditionalConditions) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalConditions), additionalConditions, "未定義の追加状態が含まれています。");
        }
    }
}

/// <summary>
/// どくやまひのように同時に複数は成立しない主要状態異常を表す。
/// </summary>
public enum PrimaryStatusAilment
{
    /// <summary>状態異常なし。</summary>
    None = 0,

    /// <summary>どく状態。</summary>
    Poison = 1,

    /// <summary>もうどく状態。</summary>
    BadlyPoisoned = 2,

    /// <summary>まひ状態。</summary>
    Paralysis = 3,

    /// <summary>やけど状態。</summary>
    Burn = 4,

    /// <summary>こおり状態。</summary>
    Freeze = 5,

    /// <summary>ねむり状態。</summary>
    Sleep = 6,
}

/// <summary>
/// 主要状態異常と併用可能な追加状態を表す。
/// </summary>
[Flags]
public enum AdditionalBattleCondition
{
    /// <summary>追加状態なし。</summary>
    None = 0,

    /// <summary>こんらん状態。</summary>
    Confusion = 1,

    /// <summary>やどりぎのたね状態。</summary>
    LeechSeed = 2,
}