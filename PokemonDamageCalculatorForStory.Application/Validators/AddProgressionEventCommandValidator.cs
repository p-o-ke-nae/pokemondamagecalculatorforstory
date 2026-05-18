using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class AddProgressionEventCommandValidator : AbstractValidator<AddProgressionEventCommand>
{
    public AddProgressionEventCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.BattleId).NotEmpty();
        RuleFor(x => x.Species).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).InclusiveBetween(1, 100);
        RuleFor(x => x.Stats).NotEmpty();
        RuleFor(x => x.EVs).NotEmpty();
    }
}
