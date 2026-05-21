using MediatR;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Domain.Ports;

namespace PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;

public sealed record GetAdminRuleSetByIdQuery(Guid Id) : IRequest<AdminRuleSetDto?>;

public sealed class GetAdminRuleSetByIdQueryHandler(IRuleSetRepository repository) : IRequestHandler<GetAdminRuleSetByIdQuery, AdminRuleSetDto?>
{
    public async Task<AdminRuleSetDto?> Handle(GetAdminRuleSetByIdQuery request, CancellationToken cancellationToken)
    {
        var ruleSet = await repository.FindByIdAsync(request.Id, cancellationToken);

        if (ruleSet is null)
        {
            return null;
        }

        return new AdminRuleSetDto(
            ruleSet.Id,
            ruleSet.Slug,
            ruleSet.Generation,
            ruleSet.Title,
            ruleSet.Version,
            ruleSet.Status,
            ruleSet.Summary,
            await repository.IsReferencedByRunsAsync(ruleSet.Id, cancellationToken));
    }
}
