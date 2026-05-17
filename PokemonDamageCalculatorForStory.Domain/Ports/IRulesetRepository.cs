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
