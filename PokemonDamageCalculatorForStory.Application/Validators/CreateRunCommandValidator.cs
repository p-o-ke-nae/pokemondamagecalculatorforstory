using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CreateRunCommandValidator : AbstractValidator<CreateRunCommand>
{
    public CreateRunCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RuleSetId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
