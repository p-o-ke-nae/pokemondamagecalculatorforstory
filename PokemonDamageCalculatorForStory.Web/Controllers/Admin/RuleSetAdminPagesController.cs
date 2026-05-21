using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokemonDamageCalculatorForStory.Application.DTOs.Admin;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Authorization;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Extensions;
using PokemonDamageCalculatorForStory.ViewModels.Admin;

namespace PokemonDamageCalculatorForStory.Controllers.Admin;

[AutoValidateAntiforgeryToken]
[Authorize(AuthenticationSchemes = AdminCookieAuthenticationDefaults.AuthenticationScheme, Policy = AppPolicies.ManageBusinessMasters)]
public sealed class RuleSetAdminPagesController(
    IMediator mediator,
    IValidator<CreateAdminRuleSetCommand> createValidator,
    IValidator<UpdateAdminRuleSetCommand> updateValidator) : Controller
{
    [HttpGet("admin/masters/rule-sets")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await mediator.Send(new GetAdminRuleSetsQuery(), cancellationToken));

    [HttpGet("admin/masters/rule-sets/new")]
    public IActionResult Create()
        => View("Editor", CreateViewModel(null, new("", 1, "", "", RuleSetStatuses.Draft, ""), false));

    [HttpPost("admin/masters/rule-sets/new")]
    public async Task<IActionResult> Create([FromForm] AdminRuleSetUpsertRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAdminRuleSetCommand(
            User.GetRequiredGoogleUserId(),
            User.GetRoleOrMember(),
            request.Slug,
            request.Generation,
            request.Title,
            request.Version,
            request.Status,
            request.Summary);

        var validationResult = await createValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            ModelState.AddValidationFailures(validationResult.Errors);
            return View("Editor", CreateViewModel(null, request, false));
        }

        try
        {
            var created = await mediator.Send(command, cancellationToken);

            TempData["SuccessMessage"] = $"RuleSet '{created.Title}' を作成しました。";
            return RedirectToAction(nameof(Edit), new { id = created.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Editor", CreateViewModel(null, request, false));
        }
    }

    [HttpGet("admin/masters/rule-sets/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var ruleSet = await mediator.Send(new GetAdminRuleSetByIdQuery(id), cancellationToken);
        if (ruleSet is null)
        {
            return NotFound();
        }

        return View("Editor", CreateViewModel(
            ruleSet.Id,
            new AdminRuleSetUpsertRequest(ruleSet.Slug, ruleSet.Generation, ruleSet.Title, ruleSet.Version, ruleSet.Status, ruleSet.Summary),
            ruleSet.IsReferencedByRuns));
    }

    [HttpPost("admin/masters/rule-sets/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromForm] AdminRuleSetUpsertRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAdminRuleSetCommand(
            User.GetRequiredGoogleUserId(),
            User.GetRoleOrMember(),
            id,
            request.Slug,
            request.Generation,
            request.Title,
            request.Version,
            request.Status,
            request.Summary);

        var validationResult = await updateValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            ModelState.AddValidationFailures(validationResult.Errors);
            return View("Editor", CreateViewModel(id, request, false));
        }

        try
        {
            var updated = await mediator.Send(command, cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = $"RuleSet '{updated.Title}' を更新しました。";
            return RedirectToAction(nameof(Edit), new { id = updated.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Editor", CreateViewModel(id, request, false));
        }
    }

    [HttpPost("admin/masters/rule-sets/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await mediator.Send(new DeleteAdminRuleSetCommand(User.GetRequiredGoogleUserId(), User.GetRoleOrMember(), id), cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["SuccessMessage"] = "RuleSet を削除しました。";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    private static AdminRuleSetEditorViewModel CreateViewModel(Guid? id, AdminRuleSetUpsertRequest form, bool isReferencedByRuns)
        => new()
        {
            Id = id,
            Heading = id.HasValue ? "RuleSet 編集" : "RuleSet 新規作成",
            Form = form,
            IsReferencedByRuns = isReferencedByRuns,
            StatusOptions = RuleSetStatuses.AllowedValues.ToList().AsReadOnly()
        };
}
