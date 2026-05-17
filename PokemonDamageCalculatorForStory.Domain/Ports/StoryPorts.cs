using PokemonDamageCalculatorForStory.Domain.Models;

namespace PokemonDamageCalculatorForStory.Domain.Ports;

/// <summary>ルールセットおよび version set の取得と更新を行うポートです。</summary>
public interface IRulesetRepository
{
    /// <summary>利用可能なルールセット一覧を取得します。</summary>
    Task<IReadOnlyList<Ruleset>> ListRulesetsAsync(CancellationToken cancellationToken = default);

    /// <summary>指定識別子のルールセットを取得します。</summary>
    Task<Ruleset?> FindRulesetAsync(Guid rulesetId, CancellationToken cancellationToken = default);

    /// <summary>利用可能な version set 一覧を取得します。</summary>
    Task<IReadOnlyList<MasterVersionSet>> ListMasterVersionSetsAsync(CancellationToken cancellationToken = default);

    /// <summary>指定識別子の version set を取得します。</summary>
    Task<MasterVersionSet?> FindMasterVersionSetAsync(Guid versionSetId, CancellationToken cancellationToken = default);

    /// <summary>version set を保存します。</summary>
    Task SaveMasterVersionSetAsync(MasterVersionSet versionSet, CancellationToken cancellationToken = default);
}

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

/// <summary>共有 snapshot の永続化を行うポートです。</summary>
public interface IShareRepository
{
    /// <summary>指定識別子の share を取得します。</summary>
    Task<SharedRouteSnapshot?> FindShareAsync(Guid shareId, CancellationToken cancellationToken = default);

    /// <summary>share を保存します。</summary>
    Task SaveShareAsync(SharedRouteSnapshot share, CancellationToken cancellationToken = default);
}

/// <summary>import job の永続化を行うポートです。</summary>
public interface IImportJobRepository
{
    /// <summary>指定識別子の import job を取得します。</summary>
    Task<ImportJob?> FindJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>job を保存します。</summary>
    Task SaveJobAsync(ImportJob job, CancellationToken cancellationToken = default);
}
