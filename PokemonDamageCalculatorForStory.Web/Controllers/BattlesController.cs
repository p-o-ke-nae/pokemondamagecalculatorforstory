using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.DTOs;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;
using PokemonDamageCalculatorForStory.Application.UseCases.Queries;

namespace PokemonDamageCalculatorForStory.Controllers;

[ApiController]
[Route("api/runs/{runId:guid}/battles")]
public sealed class BattlesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(Guid runId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetBattlesByRunQuery(runId), cancellationToken));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Guid runId, [FromBody] CreateBattleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateBattleCommand(runId, request.EnemyPokemon, request.Sequence), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { runId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid runId, Guid id, [FromBody] UpdateBattleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateBattleCommand(runId, id, request.EnemyPokemon, request.Sequence), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid runId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteBattleCommand(runId, id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/calculate")]
    [Authorize]
    public async Task<IActionResult> Calculate(Guid runId, Guid id, [FromBody] CalculateDamageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CalculateDamageCommand(runId, id, request.AttackerLevel, request.AttackStat, request.MovePower, request.IsSpecialMove, request.HasStab, request.DefenseStat, request.TypeEffectiveness), cancellationToken);
        return Ok(result);
    }
}
