namespace HotChocolate.Transport.Serialization;

/// <summary>
/// A helper class that contains the default names of the GraphQL request properties.
/// </summary>
public static class Utf8GraphQLRequestProperties
{
    // This uses C# compiler's ability to refer to static data directly.
    // For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static

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
    /// Gets the name of the variables property.
    /// </summary>
    public static ReadOnlySpan<byte> VariablesProp => "variables"u8;

    /// <summary>
    /// Gets the name of the extensions property.
    /// </summary>
    public static ReadOnlySpan<byte> ExtensionsProp => "extensions"u8;
}
