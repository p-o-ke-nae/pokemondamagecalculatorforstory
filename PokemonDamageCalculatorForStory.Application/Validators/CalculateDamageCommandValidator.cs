using FluentValidation;
using PokemonDamageCalculatorForStory.Application.UseCases.Commands;

namespace PokemonDamageCalculatorForStory.Application.Validators;

public sealed class CalculateDamageCommandValidator : AbstractValidator<CalculateDamageCommand>
{
    public CalculateDamageCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.BattleId).NotEmpty();
        RuleFor(x => x.AttackerLevel).InclusiveBetween(1, 100);
        RuleFor(x => x.AttackStat).GreaterThan(0);
        RuleFor(x => x.MovePower).GreaterThan(0);
        RuleFor(x => x.DefenseStat).GreaterThan(0);
        RuleFor(x => x.TypeEffectiveness).GreaterThan(0);
    }
}
