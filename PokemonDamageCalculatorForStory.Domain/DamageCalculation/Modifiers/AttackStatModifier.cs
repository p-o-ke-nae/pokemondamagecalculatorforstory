namespace PokemonDamageCalculatorForStory.Domain.DamageCalculation.Modifiers;

/// <summary>
/// 攻撃値補正を表す。
/// </summary>
public readonly struct AttackStatModifier
{
    public AttackStatModifier(int numerator, int denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    public static AttackStatModifier None { get; } = new(1, 1);
    public static AttackStatModifier Boost { get; } = new(3, 2);

    public int Numerator { get; }
    public int Denominator { get; }

    public int ApplyTo(int value)
    {
        return value * Numerator / Denominator;
    }

    public AttackStatModifier Compose(AttackStatModifier other)
    {
        return new AttackStatModifier(
            Numerator * other.Numerator,
            Denominator * other.Denominator);
    }
}