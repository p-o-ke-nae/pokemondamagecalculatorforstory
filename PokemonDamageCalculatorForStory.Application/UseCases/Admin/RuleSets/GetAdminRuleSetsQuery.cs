using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;

public sealed record GetAdminRuleSetsQuery : IRequest<IReadOnlyList<AdminRuleSetDto>>;

public sealed class GetAdminRuleSetsQueryHandler(IRuleSetRepository repository) : IRequestHandler<GetAdminRuleSetsQuery, IReadOnlyList<AdminRuleSetDto>>
{
    public async Task<IReadOnlyList<AdminRuleSetDto>> Handle(GetAdminRuleSetsQuery request, CancellationToken cancellationToken)
    {
        var ruleSets = await repository.FindAllAsync(cancellationToken);
        var referencedIds = await repository.FindReferencedRuleSetIdsAsync(cancellationToken);

        return ruleSets
            .Select(ruleSet => new AdminRuleSetDto(
                ruleSet.Id,
                ruleSet.Slug,
                ruleSet.Generation,
                ruleSet.Title,
                ruleSet.Version,
                ruleSet.Status,
                ruleSet.Summary,
                referencedIds.Contains(ruleSet.Id)))
            .ToList()
            .AsReadOnly();
    }
}
