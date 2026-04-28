namespace HotChocolate.Serialization;

public struct SchemaFormatterOptions
{
    /// <summary>
    /// Master fallback for type and field ordering.
    /// Use <see cref="OrderTypesByName"/> or <see cref="OrderFieldsByName"/> to
    /// control ordering independently.
    /// </summary>
    public bool? OrderByName { get; set; }

    /// <summary>
    /// Controls whether type definitions and directive definitions are emitted in
    /// alphabetical order. When unset, falls back to <see cref="OrderByName"/>, then
    /// <c>true</c>.
    /// </summary>
    public bool? OrderTypesByName { get; set; }

    /// <summary>
    /// Controls whether fields, input fields, and enum values within a type are
    /// emitted in alphabetical order. When unset, falls back to
    /// <see cref="OrderByName"/>, then <c>true</c>.
    /// </summary>
    public bool? OrderFieldsByName { get; set; }

    public bool? Indented { get; set; }

    public bool? PrintSpecScalars { get; set; }

    public bool? PrintSpecDirectives { get; set; }
}
