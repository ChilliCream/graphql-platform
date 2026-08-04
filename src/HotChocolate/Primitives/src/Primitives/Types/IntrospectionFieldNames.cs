namespace HotChocolate.Types;

/// <summary>
/// Represents the names of the introspection fields available on composite types and on the query root type.
/// </summary>
public static class IntrospectionFieldNames
{
    /// <summary>
    /// Gets the field name of the __typename introspection field.
    /// </summary>
    public const string TypeName = "__typename";

    /// <summary>
    /// Gets the field name of the __typename introspection field as a span of utf-8 bytes.
    /// </summary>
    public static ReadOnlySpan<byte> TypeNameSpan => "__typename"u8;

    /// <summary>
    /// Gets the field name of the __schema introspection field.
    /// </summary>
    public const string Schema = "__schema";

    /// <summary>
    /// Gets the field name of the __schema introspection field as a span of utf-8 bytes.
    /// </summary>
    public static ReadOnlySpan<byte> SchemaSpan => "__schema"u8;

    /// <summary>
    /// Gets the field name of the __type introspection field.
    /// </summary>
    public const string Type = "__type";

    /// <summary>
    /// Gets the field name of the __type introspection field as a span of utf-8 bytes.
    /// </summary>
    public static ReadOnlySpan<byte> TypeSpan => "__type"u8;

    /// <summary>
    /// Gets the field name of the __search introspection field.
    /// </summary>
    public const string Search = "__search";

    /// <summary>
    /// Gets the field name of the __search introspection field as a span of utf-8 bytes.
    /// </summary>
    public static ReadOnlySpan<byte> SearchSpan => "__search"u8;

    /// <summary>
    /// Gets the field name of the __definitions introspection field.
    /// </summary>
    public const string Definitions = "__definitions";

    /// <summary>
    /// Gets the field name of the __definitions introspection field as a span of utf-8 bytes.
    /// </summary>
    public static ReadOnlySpan<byte> DefinitionsSpan => "__definitions"u8;
}
