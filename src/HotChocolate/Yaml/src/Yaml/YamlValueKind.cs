namespace HotChocolate.Text.Yaml;

/// <summary>
/// Specifies the kind of a <see cref="YamlElement"/>.
/// <see cref="Object"/> is a YAML mapping and <see cref="Array"/> is a YAML sequence.
/// </summary>
public enum YamlValueKind : byte
{
    /// <summary>
    /// The element has no value, for example a default <see cref="YamlElement"/>.
    /// </summary>
    Undefined,

    /// <summary>
    /// A mapping of keys to values.
    /// </summary>
    Object,

    /// <summary>
    /// A sequence of values.
    /// </summary>
    Array,

    /// <summary>
    /// A string.
    /// </summary>
    String,

    /// <summary>
    /// A plain scalar that the YAML 1.2.2 core schema resolves to an integer or a float.
    /// </summary>
    Number,

    /// <summary>
    /// The plain scalar <c>true</c>, <c>True</c> or <c>TRUE</c>.
    /// </summary>
    True,

    /// <summary>
    /// The plain scalar <c>false</c>, <c>False</c> or <c>FALSE</c>.
    /// </summary>
    False,

    /// <summary>
    /// The plain scalar <c>null</c>, <c>Null</c>, <c>NULL</c> or <c>~</c>, or a key or sequence
    /// item without a value.
    /// </summary>
    Null
}
