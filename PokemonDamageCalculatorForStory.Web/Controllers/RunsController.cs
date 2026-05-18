using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;
using PokemonDamageCalculatorForStory.Application.UseCases.Queries;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Controllers;

[ApiController]
[Route("api/runs")]
public sealed class RunsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAllRunsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRunByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRunRequest request, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredGoogleUserId();
        var result = await mediator.Send(new CreateRunCommand(ownerUserId, request.Name, request.RuleSetId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRunRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateRunCommand(id, request.Name, request.Status), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteRunCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{runId:guid}/party-state")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPartyState(Guid runId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new ProjectPartyStateQuery(runId), cancellationToken));

    [HttpPost("{runId:guid}/party-state")]
    [Authorize]
    public async Task<IActionResult> AddProgressionEvent(Guid runId, [FromBody] AddProgressionEventRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddProgressionEventCommand(runId, request.BattleId, request.Species, request.Level, request.Stats, request.EVs), cancellationToken);
        return Ok(result);
    }
}
