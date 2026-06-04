using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class UpdateBattleCommandValidator : AbstractValidator<UpdateBattleCommand>
{
    public UpdateBattleCommandValidator()
    {
        RuleFor(x => x.EnemyPokemon).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Sequence).GreaterThan(0);
    }
}
