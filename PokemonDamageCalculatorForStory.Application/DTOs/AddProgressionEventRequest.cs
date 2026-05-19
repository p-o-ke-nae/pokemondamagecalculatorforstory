namespace PokemonDamageCalculatorForStory.Application.DTOs;

/// <summary>
/// 手持ちポケモンスナップショット追加リクエストを表します。
/// </summary>
/// <param name="BattleId">対応する戦闘 ID。</param>
/// <param name="Species">ポケモン種族名。</param>
/// <param name="Level">レベル。</param>
/// <param name="BaseStats">種族値 JSON。</param>
/// <param name="IVs">個体値 JSON。</param>
/// <param name="Stats">実数値 JSON。</param>
/// <param name="EVs">努力値 JSON。</param>
public sealed record AddProgressionEventRequest(Guid BattleId, string Species, int Level, string BaseStats, string IVs, string Stats, string EVs);
