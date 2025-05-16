namespace HotChocolate.Types;

/// <summary>
/// Represents the names of the introspection fields available on composite types and on the query root type.
/// </summary>
public static class IntrospectionFieldNames
{
    /// <summary>
    /// Gets the field name of the __typename introspection field.
    /// </summary>
    public static string TypeName => "__typename";

    /// <summary>
    /// Gets the field name of the __schema introspection field.
    /// </summary>
    public static string Schema => "__schema";

    /// <summary>
    /// Gets the field name of the __type introspection field.
    /// </summary>
    public static string Type => "__type";
}