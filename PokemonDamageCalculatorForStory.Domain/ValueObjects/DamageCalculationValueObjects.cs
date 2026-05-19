using PokemonDamageCalculatorForStory.Domain.Exceptions;

namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

public readonly record struct PokemonLevel
{
    public const int MinValue = 1;
    public const int MaxValue = 100;

    public int Value { get; }

    private PokemonLevel(int value)
    {
        Value = value;
    }

    public static PokemonLevel Create(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new ValidationException($"Level must be between {MinValue} and {MaxValue}.");
        }

        return new PokemonLevel(value);
    }
}

public readonly record struct AttackStat
{
    public int Value { get; }

    private AttackStat(int value)
    {
        Value = value;
    }

    public static AttackStat Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("AttackStat must be greater than zero.");
        }

        return new AttackStat(value);
    }
}

public readonly record struct DefenseStat
{
    public int Value { get; }

    private DefenseStat(int value)
    {
        Value = value;
    }

    public static DefenseStat Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("DefenseStat must be greater than zero.");
        }

        return new DefenseStat(value);
    }
}

public readonly record struct MovePower
{
    public int Value { get; }

    private MovePower(int value)
    {
        Value = value;
    }

    public static MovePower Create(int value)
    {
        if (value <= 0)
        {
            throw new ValidationException("MovePower must be greater than zero.");
        }

        return new MovePower(value);
    }
}

public readonly record struct TypeEffectivenessMultiplier
{
    public float Value { get; }

    private TypeEffectivenessMultiplier(float value)
    {
        Value = value;
    }

    public static TypeEffectivenessMultiplier Create(float value)
    {
        if (value <= 0)
        {
            throw new ValidationException("TypeEffectiveness must be greater than zero.");
        }

        return new TypeEffectivenessMultiplier(value);
    }
}
