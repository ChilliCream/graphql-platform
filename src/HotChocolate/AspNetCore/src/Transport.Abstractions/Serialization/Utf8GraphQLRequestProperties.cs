namespace HotChocolate.Transport.Serialization;

/// <summary>
/// A helper class that contains the default names of the GraphQL request properties.
/// </summary>
public static class Utf8GraphQLRequestProperties
{
    /// <summary>
    /// Gets the name of the id property.
    /// </summary>
    public static ReadOnlySpan<byte> IdProp => "id"u8;

    /// <summary>
    /// Gets the name of the query property.
    /// </summary>
    public static ReadOnlySpan<byte> QueryProp => "query"u8;

    /// <summary>
    /// Gets the name of the operationName property.
    /// </summary>
    public static ReadOnlySpan<byte> OperationNameProp => "operationName"u8;

    /// <summary>
    /// Gets the name of the onError property.
    /// </summary>
    public static ReadOnlySpan<byte> OnErrorProp => "onError"u8;

    /// <summary>
    /// Gets the name of the variables property.
    /// </summary>
    public static ReadOnlySpan<byte> VariablesProp => "variables"u8;

    /// <summary>
    /// Gets the name of the extensions property.
    /// </summary>
    public static ReadOnlySpan<byte> ExtensionsProp => "extensions"u8;
}
