using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

/// <summary>run 集約の永続化を行うポートです。</summary>
public interface IRunRepository
{
    /// <summary>指定ユーザーの run 一覧を取得します。</summary>
    Task<IReadOnlyList<RunAggregate>> ListRunsAsync(string ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>指定識別子の run を取得します。</summary>
    Task<RunAggregate?> FindRunAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>run を保存します。</summary>
    Task SaveRunAsync(RunAggregate run, CancellationToken cancellationToken = default);
}
