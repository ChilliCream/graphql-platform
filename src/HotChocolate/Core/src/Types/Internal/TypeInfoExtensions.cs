using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// Provides extension methods for <see cref="ITypeInfo"/>.
/// </summary>
public static class TypeInfoExtensions
{
    /// <summary>
    /// Creates a type structure with the <paramref name="typeDefinition"/>.
    /// </summary>
    /// <param name="typeInfo">The type info.</param>
    /// <param name="typeDefinition">The type definition.</param>
    /// <returns>
    /// Returns a GraphQL type structure.
    /// </returns>
    internal static IType CreateType(this ITypeInfo typeInfo, ITypeDefinition typeDefinition)
    {
        if (typeInfo is ITypeFactory factory)
        {
            return factory.CreateType(typeDefinition);
        }

        throw new NotSupportedException(
            "The specified type info does not support creating new instances.");
    }

    /// <summary>
    /// Helper to rewrite a type structure.
    /// </summary>
    public static TypeReference CreateTypeReference(this ITypeInfo typeInfo, NamedTypeNode namedType)
    {
        ArgumentNullException.ThrowIfNull(namedType);

        ITypeNode type = namedType;

        for (var i = typeInfo.Components.Count - 1; i >= 0; i--)
        {
            var component = typeInfo.Components[i];
            switch (component.Kind)
            {
                case TypeComponentKind.Named:
                    continue;

                case TypeComponentKind.NonNull:
                    type = new NonNullTypeNode((INullableTypeNode)type);
                    break;

                case TypeComponentKind.List:
                    type = new ListTypeNode(type);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        return TypeReference.Create(type);
    }
}
