using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Queries;

public sealed class GetRuleSetByIdQueryHandler(IRuleSetRepository repository) : IRequestHandler<GetRuleSetByIdQuery, RuleSetDto?>
{
    public async Task<RuleSetDto?> Handle(GetRuleSetByIdQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await repository.FindActiveByIdAsync(request.Id, cancellationToken);
        return ruleSet is null ? null : new RuleSetDto(ruleSet.Id, ruleSet.Slug, ruleSet.Generation, ruleSet.Title, ruleSet.Version, ruleSet.Status, ruleSet.Summary);
    }
}
