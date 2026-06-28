namespace PokemonDamageCalculatorForStory.Domain.Entities;

/// <summary>
/// ポケモンの特性を表す。
/// </summary>
public class Ability
{
    /// <summary>
    /// 特性を初期化する。
    /// </summary>
    /// <param name="name">特性名。</param>
    public Ability(string name)
    {
        Name = name;
    }

    /// <summary>特性名。</summary>
    public string Name { get; }
}