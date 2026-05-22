using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Extensions;

namespace PokemonDamageCalculatorForStory.Controllers.Admin;

[ApiController]
[Route("api/admin/user-authorizations")]
[Authorize(Policy = AppPolicies.ManageAuthorizationMasters)]
public sealed class UserAuthorizationsAdminController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAdminUserAuthorizationsQuery(), cancellationToken));

    [HttpGet("{googleUserId}")]
    public async Task<IActionResult> GetById(string googleUserId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAdminUserAuthorizationByIdQuery(googleUserId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminUserAuthorizationUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), request.GoogleUserId, request.Role, request.Permissions),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { googleUserId = result.GoogleUserId }, result);
    }

    [HttpPut("{googleUserId}")]
    public async Task<IActionResult> Update(string googleUserId, [FromBody] AdminUserAuthorizationUpsertRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), googleUserId, request.Role, request.Permissions),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{googleUserId}")]
    public async Task<IActionResult> Delete(string googleUserId, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(
            new DeleteAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), googleUserId),
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
