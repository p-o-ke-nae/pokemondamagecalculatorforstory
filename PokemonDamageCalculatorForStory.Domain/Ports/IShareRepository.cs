using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

/// <summary>共有 snapshot の永続化を行うポートです。</summary>
public interface IShareRepository
{
    /// <summary>指定識別子の share を取得します。</summary>
    Task<SharedRouteSnapshot?> FindShareAsync(Guid shareId, CancellationToken cancellationToken = default);

    /// <summary>share を保存します。</summary>
    Task SaveShareAsync(SharedRouteSnapshot share, CancellationToken cancellationToken = default);
}
