using PokemonDamageCalculatorForStory.Domain.Exceptions;
using PokemonDamageCalculatorForStory.Domain.ValueObjects;

namespace PokemonDamageCalculatorForStory.Domain.Entities;

public sealed class OwnPokemonSnapshot
{
    public const int SpeciesMaxLength = 100;

    public Guid Id { get; private set; }
    public Guid BattleId { get; private set; }
    public string Species { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public SpeciesBaseStats? BaseStats { get; private set; }
    public IndividualValues? IVs { get; private set; }
    public PokemonStats Stats { get; private set; }
    public EffortValues EVs { get; private set; }

    private OwnPokemonSnapshot()
    {
    }

    /// <summary>
    /// 手持ちポケモンのスナップショットを新規作成します。
    /// </summary>
    /// <param name="battleId">対応する戦闘 ID。</param>
    /// <param name="species">ポケモン種族名。</param>
    /// <param name="level">レベル。</param>
    /// <param name="baseStats">種族値。</param>
    /// <param name="iVs">個体値。</param>
    /// <param name="stats">実数値。</param>
    /// <param name="eVs">努力値。</param>
    /// <returns>生成されたスナップショット。</returns>
    public static OwnPokemonSnapshot Create(Guid battleId, string species, int level, SpeciesBaseStats baseStats, IndividualValues iVs, PokemonStats stats, EffortValues eVs)
    {
        var validatedLevel = PokemonLevel.Create(level);

        if (battleId == Guid.Empty) throw new ValidationException("BattleId is required.");
        if (string.IsNullOrWhiteSpace(species)) throw new ValidationException("Species is required.");

        var normalizedSpecies = species.Trim();
        if (normalizedSpecies.Length > SpeciesMaxLength) throw new ValidationException($"Species must be {SpeciesMaxLength} characters or fewer.");

        return new OwnPokemonSnapshot
        {
            Id = Guid.NewGuid(),
            BattleId = battleId,
            Species = normalizedSpecies,
            Level = validatedLevel.Value,
            BaseStats = baseStats,
            IVs = iVs,
            Stats = stats,
            EVs = eVs
        };
    }

    /// <summary>
    /// 永続化済みデータから手持ちポケモンのスナップショットを復元します。
    /// </summary>
    /// <param name="id">スナップショット ID。</param>
    /// <param name="battleId">対応する戦闘 ID。</param>
    /// <param name="species">ポケモン種族名。</param>
    /// <param name="level">レベル。</param>
    /// <param name="baseStats">種族値。</param>
    /// <param name="iVs">個体値。</param>
    /// <param name="stats">実数値。</param>
    /// <param name="eVs">努力値。</param>
    /// <returns>復元されたスナップショット。</returns>
    public static OwnPokemonSnapshot Restore(Guid id, Guid battleId, string species, int level, SpeciesBaseStats? baseStats, IndividualValues? iVs, PokemonStats stats, EffortValues eVs)
    {
        return new OwnPokemonSnapshot
        {
            Id = id,
            BattleId = battleId,
            Species = species.Trim(),
            Level = level,
            BaseStats = baseStats,
            IVs = iVs,
            Stats = stats,
            EVs = eVs
        };
    }
}
