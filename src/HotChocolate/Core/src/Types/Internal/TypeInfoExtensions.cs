using HotChocolate.Types;

namespace HotChocolate.Internal;

internal static class TypeInfoExtensions
{
    /// <summary>
    /// Creates a type structure with the <paramref name="namedType"/>.
    /// </summary>
    /// <param name="typeInfo">The type info.</param>
    /// <param name="namedType">The named type component.</param>
    /// <returns>
    /// Returns a GraphQL type structure.
    /// </returns>
    internal static IType CreateType(this ITypeInfo typeInfo, INamedType namedType)
    {
        if (typeInfo is ITypeFactory factory)
        {
            return factory.CreateType(namedType);
        }

        throw new NotSupportedException(
            "The specified type info does not support creating new instances.");
    }
}
