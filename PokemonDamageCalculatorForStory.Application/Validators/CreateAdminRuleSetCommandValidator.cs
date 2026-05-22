using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.RuleSets;
using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CreateAdminRuleSetCommandValidator : AbstractValidator<CreateAdminRuleSetCommand>
{
    public CreateAdminRuleSetCommandValidator()
    {
        this.ConfigureRuleSetValidation();
    }
}

public sealed class UpdateAdminRuleSetCommandValidator : AbstractValidator<UpdateAdminRuleSetCommand>
{
    public UpdateAdminRuleSetCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        this.ConfigureRuleSetValidation();
    }
}

internal static class AdminRuleSetValidationExtensions
{
    public static void ConfigureRuleSetValidation(this AbstractValidator<CreateAdminRuleSetCommand> validator)
    {
        validator.RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        validator.RuleFor(x => x.Generation).GreaterThan(0);
        validator.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        validator.RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        validator.RuleFor(x => x.Status).Must(RuleSetStatuses.AllowedValues.Contains).WithMessage("Status must be Draft, Active, or Archived.");
        validator.RuleFor(x => x.Summary).MaximumLength(500);
    }

    public static void ConfigureRuleSetValidation(this AbstractValidator<UpdateAdminRuleSetCommand> validator)
    {
        validator.RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
        validator.RuleFor(x => x.Generation).GreaterThan(0);
        validator.RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        validator.RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        validator.RuleFor(x => x.Status).Must(RuleSetStatuses.AllowedValues.Contains).WithMessage("Status must be Draft, Active, or Archived.");
        validator.RuleFor(x => x.Summary).MaximumLength(500);
    }
}
