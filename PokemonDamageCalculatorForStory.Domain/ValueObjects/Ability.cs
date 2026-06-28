using PokemonDamageCalculatorForStory.Domain.DamageCalculation.AbilityEffects;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの特性を表す。
/// </summary>
public class Ability
{
    private readonly IAbilityDamageEffect[] _damageEffects;
    private readonly IAbilityStatusAilmentEffect[] _statusAilmentEffects;
    private readonly IAbilityAttackStatEffect[] _attackStatEffects;

    public Ability(
        string name,
        IAbilityDamageEffect[]? damageEffects = null,
        IAbilityStatusAilmentEffect[]? statusAilmentEffects = null,
        IAbilityAttackStatEffect[]? attackStatEffects = null)
    {
        Name = name;
        _damageEffects = damageEffects ?? [];
        _statusAilmentEffects = statusAilmentEffects ?? [];
        _attackStatEffects = attackStatEffects ?? [];
    }

    public string Name { get; }

    public IAbilityDamageEffect[] DamageEffects => _damageEffects.ToArray();
    public IAbilityStatusAilmentEffect[] StatusAilmentEffects => _statusAilmentEffects.ToArray();
    public IAbilityAttackStatEffect[] AttackStatEffects => _attackStatEffects.ToArray();
}