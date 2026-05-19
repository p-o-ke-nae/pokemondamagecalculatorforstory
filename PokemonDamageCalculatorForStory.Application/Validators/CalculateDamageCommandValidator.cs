using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CalculateDamageCommandValidator : AbstractValidator<CalculateDamageCommand>
{
    public CalculateDamageCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.BattleId).NotEmpty();
    }
}
