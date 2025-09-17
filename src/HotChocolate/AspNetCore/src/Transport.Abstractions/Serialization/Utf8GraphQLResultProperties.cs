namespace HotChocolate.Transport.Serialization;

/// <summary>
/// This helper class contains the default property names for the GraphQL result object.
/// </summary>
internal static class Utf8GraphQLResultProperties
{
    /// <summary>
    /// Gets the data property name.
    /// </summary>
    public static ReadOnlySpan<byte> DataProp => "data"u8;

    /// <summary>
    /// Gets the errors property name.
    /// </summary>
    public static ReadOnlySpan<byte> ErrorsProp => "errors"u8;

    /// <summary>
    /// Gets the extensions property name.
    /// </summary>
    public static ReadOnlySpan<byte> ExtensionsProp => "extensions"u8;

    /// <summary>
    /// Gets the request index property name.
    /// </summary>
    public static ReadOnlySpan<byte> RequestIndexProp => "requestIndex"u8;

    /// <summary>
    /// Gets the variable index property name.
    /// </summary>
    public static ReadOnlySpan<byte> VariableIndexProp => "variableIndex"u8;
}
