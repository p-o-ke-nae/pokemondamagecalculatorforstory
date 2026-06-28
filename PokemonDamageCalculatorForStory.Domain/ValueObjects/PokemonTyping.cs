namespace PokemonDamageCalculatorForStory.Domain.ValueObjects;

/// <summary>
/// ポケモンの元タイプと現在タイプを保持する。
/// </summary>
public class PokemonTyping
{
    private readonly PokemonType[] _baseTypes;
    private PokemonType[] _currentTypes;

    /// <summary>
    /// タイプ情報を初期化する。
    /// </summary>
    /// <param name="baseTypes">元のタイプ配列。</param>
    public PokemonTyping(PokemonType[] baseTypes)
    {
        _baseTypes = baseTypes.ToArray();
        _currentTypes = baseTypes.ToArray();
    }

    /// <summary>元のタイプ配列。</summary>
    public PokemonType[] BaseTypes => _baseTypes.ToArray();

    /// <summary>現在のタイプ配列。</summary>
    public PokemonType[] CurrentTypes => _currentTypes.ToArray();

    /// <summary>
    /// 現在のタイプを指定した配列に置き換える。
    /// </summary>
    /// <param name="types">置き換え後のタイプ配列。</param>
    public void ReplaceWith(PokemonType[] types)
    {
        _currentTypes = types.ToArray();
    }

    /// <summary>
    /// 現在のタイプを元のタイプへ戻す。
    /// </summary>
    public void Reset()
    {
        _currentTypes = _baseTypes.ToArray();
    }
}