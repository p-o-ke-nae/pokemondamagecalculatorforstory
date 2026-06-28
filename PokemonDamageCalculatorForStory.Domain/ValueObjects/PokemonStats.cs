using PokemonDamageCalculatorForStory.Domain.ValueObjects;

public sealed class PokemonStats
{
    public PokemonStats(
        int hp,
        int attack,
        int defense,
        int specialAttack,
        int specialDefense,
        int speed)
    {
        Hp = hp;
        Attack = attack;
        Defense = defense;
        SpecialAttack = specialAttack;
        SpecialDefense = specialDefense;
        Speed = speed;
    }

    /// <summary>
    /// HP。
    /// </summary>
    public int Hp { get; }
    /// <summary>
    /// こうげき。
    /// </summary>
    public int Attack { get; }
    /// <summary>
    /// ぼうぎょ。
    /// </summary>
    public int Defense { get; }
    /// <summary>
    /// とくこう。
    /// </summary>
    public int SpecialAttack { get; }
    /// <summary>
    /// とくぼう。
    /// </summary>
    public int SpecialDefense { get; }
    /// <summary>
    /// すばやさ。
    /// </summary>
    public int Speed { get; }

    public int Get(StatSelector selector) =>
        selector switch
        {
            StatSelector.Hp => Hp,
            StatSelector.Attack => Attack,
            StatSelector.Defense => Defense,
            StatSelector.SpecialAttack => SpecialAttack,
            StatSelector.SpecialDefense => SpecialDefense,
            StatSelector.Speed => Speed,
            _ => throw new ArgumentOutOfRangeException(nameof(selector))
        };
}