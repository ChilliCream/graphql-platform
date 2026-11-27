namespace HotChocolate.Text.Yaml;

/// <summary>
/// A key and its value in a YAML mapping.
/// </summary>
public readonly struct YamlProperty
{
    private readonly YamlDocument _parent;
    private readonly int _index;

    internal YamlProperty(YamlDocument parent, int index)
    {
        _parent = parent;
        _index = index;
    }

    /// <summary>
    /// Gets the key of the property.
    /// </summary>
    public string Name => _parent.GetString(_index);

    /// <summary>
    /// Gets the value of the property.
    /// </summary>
    public YamlElement Value => new(_parent, _index + 1);

    /// <summary>
    /// Gets the 1-based line of the key.
    /// </summary>
    public int Line => _parent.GetRow(_index).Line;

    /// <summary>
    /// Gets the 1-based column of the key.
    /// </summary>
    public int Column => _parent.GetRow(_index).Column;
}
