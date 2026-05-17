using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

/// <summary>import job の永続化を行うポートです。</summary>
public interface IImportJobRepository
{
    /// <summary>指定識別子の import job を取得します。</summary>
    Task<ImportJob?> FindJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>job を保存します。</summary>
    Task SaveJobAsync(ImportJob job, CancellationToken cancellationToken = default);
}
