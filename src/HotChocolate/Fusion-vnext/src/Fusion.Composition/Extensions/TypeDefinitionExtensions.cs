using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    public static ITypeDefinition InnerNullableType(this ITypeDefinition type)
    {
        return type switch
        {
            ListType listType => listType.ElementType.NullableType(),
            NonNullType nonNullType => nonNullType.NullableType,
            _ => type
        };
    }
}
