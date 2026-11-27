namespace HotChocolate.Text.Yaml;

/// <summary>
/// Specifies the kind of a <see cref="YamlElement"/>.
/// </summary>
public enum YamlValueKind : byte
{
    /// <summary>
    /// The element has no assigned value.
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
    /// A number.
    /// </summary>
    Number,

    /// <summary>
    /// The boolean value <see langword="true"/>.
    /// </summary>
    True,

    /// <summary>
    /// The boolean value <see langword="false"/>.
    /// </summary>
    False,

    /// <summary>
    /// A null value, including a mapping entry or sequence item with an omitted value.
    /// </summary>
    Null
}
