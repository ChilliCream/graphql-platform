namespace HotChocolate.Serialization;

public sealed class SchemaFormatterOptions
{
    /// <summary>
    /// Controls whether type definitions and directive definitions are emitted
    /// in alphabetical order.
    /// When <c>null</c>, the value is resolved from the schema-level
    /// <see cref="SchemaFormatterOptions"/> feature, falling back to <c>true</c>.
    /// </summary>
    public bool? OrderTypesByName { get; set; }

    /// <summary>
    /// Controls whether fields, input fields, and enum values within a type are
    /// emitted in alphabetical order.
    /// When <c>null</c>, the value is resolved from the schema-level
    /// <see cref="SchemaFormatterOptions"/> feature, falling back to <c>true</c>.
    /// </summary>
    public bool? OrderFieldsByName { get; set; }

    /// <summary>
    /// Controls whether the output is indented and pretty-printed.
    /// Default: <c>true</c>.
    /// </summary>
    public bool Indented { get; set; } = true;

    /// <summary>
    /// Controls whether GraphQL spec scalars (String, Int, Float, Boolean, ID)
    /// are emitted as type definitions.
    /// Default: <c>false</c>.
    /// </summary>
    public bool PrintSpecScalars { get; set; }

    /// <summary>
    /// Controls whether GraphQL spec directives (@skip, @include, @deprecated,
    /// @specifiedBy, @oneOf) are emitted as directive definitions.
    /// Default: <c>false</c>.
    /// </summary>
    public bool PrintSpecDirectives { get; set; }

    /// <summary>
    /// Controls whether directive definitions and applied directives whose
    /// definition has <see cref="HotChocolate.Types.IDirectiveDefinition.IsPublic"/>
    /// set to <c>false</c> are included in the output.
    /// Default: <c>true</c>.
    /// </summary>
    public bool IncludeInternalDirectives { get; set; } = true;
}
