using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    public static ITypeDefinition InnerNullableType(this ITypeDefinition type)
    {
        return type switch
        {
            ListTypeDefinition listType => listType.ElementType.NullableType(),
            NonNullTypeDefinition nonNullType => nonNullType.NullableType,
            _ => type
        };
    }
}
