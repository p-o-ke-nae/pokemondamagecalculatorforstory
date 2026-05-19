using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Commands;

/// <summary>
/// 手持ちポケモンスナップショット追加コマンドを表します。
/// </summary>
/// <param name="RunId">対象 Run ID。</param>
/// <param name="BattleId">対応する戦闘 ID。</param>
/// <param name="Species">ポケモン種族名。</param>
/// <param name="Level">レベル。</param>
/// <param name="BaseStats">種族値 JSON。</param>
/// <param name="IVs">個体値 JSON。</param>
/// <param name="Stats">実数値 JSON。</param>
/// <param name="EVs">努力値 JSON。</param>
public sealed record AddProgressionEventCommand(Guid RunId, Guid BattleId, string Species, int Level, string BaseStats, string IVs, string Stats, string EVs) : IRequest<OwnPokemonSnapshotDto>;
