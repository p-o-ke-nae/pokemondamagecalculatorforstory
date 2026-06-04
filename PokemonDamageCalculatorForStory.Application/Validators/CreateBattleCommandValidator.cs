using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CreateBattleCommandValidator : AbstractValidator<CreateBattleCommand>
{
    public CreateBattleCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EnemyPokemon).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sequence).GreaterThan(0);
    }
}
