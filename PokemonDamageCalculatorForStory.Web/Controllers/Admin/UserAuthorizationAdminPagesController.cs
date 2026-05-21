using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Extensions;
using PokemonDamageCalculatorForStory.ViewModels.Admin;

namespace PokemonDamageCalculatorForStory.Controllers.Admin;

[Authorize(AuthenticationSchemes = AdminCookieAuthenticationDefaults.AuthenticationScheme, Policy = AppPolicies.ManageAuthorizationMasters)]
public sealed class UserAuthorizationAdminPagesController(IMediator mediator) : Controller
{
    [HttpGet("admin/masters/user-authorizations")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await mediator.Send(new GetAdminUserAuthorizationsQuery(), cancellationToken));

    [HttpGet("admin/masters/user-authorizations/new")]
    public IActionResult Create()
        => View("Editor", CreateViewModel(
            null,
            new AdminUserAuthorizationUpsertRequest("", AppRoles.Member, []),
            false));

    [HttpPost("admin/masters/user-authorizations/new")]
    public async Task<IActionResult> Create([FromForm] string googleUserId, [FromForm] string role, [FromForm] List<string> permissions, CancellationToken cancellationToken)
    {
        var request = new AdminUserAuthorizationUpsertRequest(googleUserId, role, permissions);

        try
        {
            var created = await mediator.Send(
                new CreateAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), request.GoogleUserId, request.Role, request.Permissions),
                cancellationToken);

            TempData["SuccessMessage"] = $"User Authorization '{created.GoogleUserId}' を作成しました。";
            return RedirectToAction(nameof(Edit), new { googleUserId = created.GoogleUserId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Editor", CreateViewModel(null, request, false));
        }
    }

    [HttpGet("admin/masters/user-authorizations/{googleUserId}")]
    public async Task<IActionResult> Edit(string googleUserId, CancellationToken cancellationToken)
    {
        var userAuthorization = await mediator.Send(new GetAdminUserAuthorizationByIdQuery(googleUserId), cancellationToken);
        if (userAuthorization is null)
        {
            return NotFound();
        }

        return View("Editor", CreateViewModel(
            userAuthorization.GoogleUserId,
            new AdminUserAuthorizationUpsertRequest(userAuthorization.GoogleUserId, userAuthorization.Role, userAuthorization.Permissions),
            userAuthorization.IsLastAdministrator));
    }

    [HttpPost("admin/masters/user-authorizations/{googleUserId}")]
    public async Task<IActionResult> Update(string googleUserId, [FromForm] string role, [FromForm] List<string> permissions, CancellationToken cancellationToken)
    {
        var request = new AdminUserAuthorizationUpsertRequest(googleUserId, role, permissions);

        try
        {
            var updated = await mediator.Send(
                new UpdateAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), googleUserId, request.Role, request.Permissions),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"User Authorization '{updated.GoogleUserId}' を更新しました。";
            return RedirectToAction(nameof(Edit), new { googleUserId = updated.GoogleUserId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Editor", CreateViewModel(googleUserId, request, false));
        }
    }

    [HttpPost("admin/masters/user-authorizations/{googleUserId}/delete")]
    public async Task<IActionResult> Delete(string googleUserId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await mediator.Send(new DeleteAdminUserAuthorizationCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), googleUserId), cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "User Authorization を削除しました。";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { googleUserId });
        }
    }

    private static AdminUserAuthorizationEditorViewModel CreateViewModel(string? googleUserId, AdminUserAuthorizationUpsertRequest form, bool isLastAdministrator)
        => new()
        {
            GoogleUserId = googleUserId,
            Heading = string.IsNullOrWhiteSpace(googleUserId) ? "User Authorization 新規作成" : "User Authorization 編集",
            Form = form,
            IsLastAdministrator = isLastAdministrator,
            RoleOptions = AppRoles.AllowedValues.ToList().AsReadOnly(),
            PermissionOptions = AppPermissions.AllowedValues.ToList().AsReadOnly()
        };
}
