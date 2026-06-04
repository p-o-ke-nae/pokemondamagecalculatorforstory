using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Controllers.Admin;

[ApiController]
[Route("api/admin/rule-sets")]
[Authorize(Policy = AppPolicies.ManageBusinessMasters)]
public sealed class RuleSetsAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAdminRuleSetsQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAdminRuleSetByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminRuleSetUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateAdminRuleSetCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), request.Slug, request.Generation, request.Title, request.Version, request.Status, request.Summary),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminRuleSetUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAdminRuleSetCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), id, request.Slug, request.Generation, request.Title, request.Version, request.Status, request.Summary),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(
            new DeleteAdminRuleSetCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), id),
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
