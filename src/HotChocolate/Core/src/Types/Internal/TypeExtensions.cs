namespace HotChocolate.Internal;

/// <summary>
/// Provides extension methods for checking if <see cref="System.Type" />
/// represents a GraphQL type system member.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is a GraphQL schema type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the specified type is a GraphQL schema type; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSchemaType(this Type type)
        => ExtendedType.Tools.IsSchemaType(type);

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is a non-generic GraphQL schema type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <c>true</c> if the specified type is a non-generic GraphQL schema type;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsNonGenericSchemaType(this Type type)
        => ExtendedType.Tools.IsNonGenericBaseType(type);
}
