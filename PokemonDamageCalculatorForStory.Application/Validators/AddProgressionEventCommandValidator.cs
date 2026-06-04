using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class AddProgressionEventCommandValidator : AbstractValidator<AddProgressionEventCommand>
{
    public AddProgressionEventCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.BattleId).NotEmpty();
    }
}
