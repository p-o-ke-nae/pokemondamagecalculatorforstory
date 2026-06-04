using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class GetAllRuleSetsQueryHandler(IRuleSetRepository repository) : IRequestHandler<GetAllRuleSetsQuery, IReadOnlyList<RuleSetDto>>
{
    public async Task<IReadOnlyList<RuleSetDto>> Handle(GetAllRuleSetsQuery request, CancellationToken cancellationToken)
    {
        var ruleSets = await repository.FindAllActiveAsync(cancellationToken);
        return ruleSets.Select(ruleSet => new RuleSetDto(ruleSet.Id, ruleSet.Slug, ruleSet.Generation, ruleSet.Title, ruleSet.Version, ruleSet.Status, ruleSet.Summary)).ToList().AsReadOnly();
    }
}
