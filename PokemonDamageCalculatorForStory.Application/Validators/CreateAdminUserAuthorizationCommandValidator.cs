using FluentValidation;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Application.UseCases.Admin.UserAuthorizations;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CreateAdminUserAuthorizationCommandValidator : AbstractValidator<CreateAdminUserAuthorizationCommand>
{
    public CreateAdminUserAuthorizationCommandValidator()
    {
        this.ConfigureValidation();
    }
}

public sealed class UpdateAdminUserAuthorizationCommandValidator : AbstractValidator<UpdateAdminUserAuthorizationCommand>
{
    public UpdateAdminUserAuthorizationCommandValidator()
    {
        this.ConfigureValidation();
    }
}

internal static class AdminUserAuthorizationValidationExtensions
{
    public static void ConfigureValidation(this AbstractValidator<CreateAdminUserAuthorizationCommand> validator)
    {
        validator.RuleFor(x => x.GoogleUserId).NotEmpty().MaximumLength(128);
        validator.RuleFor(x => x.Role).Must(AppRoles.AllowedValues.Contains).WithMessage("Role must be Administrator, MasterEditor, or Member.");
        validator.RuleFor(x => x.Permissions)
            .NotNull()
            .Must(permissions => permissions.Count == permissions.Distinct(StringComparer.Ordinal).Count())
            .WithMessage("Permissions must be unique.");
        validator.RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(128)
            .Must(AppPermissions.AllowedValues.Contains)
            .WithMessage("Permission is not part of the allowed catalog.");
    }

    public static void ConfigureValidation(this AbstractValidator<UpdateAdminUserAuthorizationCommand> validator)
    {
        validator.RuleFor(x => x.GoogleUserId).NotEmpty().MaximumLength(128);
        validator.RuleFor(x => x.Role).Must(AppRoles.AllowedValues.Contains).WithMessage("Role must be Administrator, MasterEditor, or Member.");
        validator.RuleFor(x => x.Permissions)
            .NotNull()
            .Must(permissions => permissions.Count == permissions.Distinct(StringComparer.Ordinal).Count())
            .WithMessage("Permissions must be unique.");
        validator.RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(128)
            .Must(AppPermissions.AllowedValues.Contains)
            .WithMessage("Permission is not part of the allowed catalog.");
    }
}
