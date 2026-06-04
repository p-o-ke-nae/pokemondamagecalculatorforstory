using MediatR;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.UseCases.Queries;

namespace PokemonDamageCalculatorForStory.Controllers;

[ApiController]
[Route("api/rule-sets")]
public sealed class RuleSetsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAllRuleSetsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRuleSetByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
