namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンのもちものを表す。
/// </summary>
public class Item
{
    /// <summary>
    /// もちものを初期化する。
    /// </summary>
    /// <param name="name">もちもの名。</param>
    public Item(string name)
    {
        Name = name;
    }

    /// <summary>もちもの名。</summary>
    public string Name { get; }
}