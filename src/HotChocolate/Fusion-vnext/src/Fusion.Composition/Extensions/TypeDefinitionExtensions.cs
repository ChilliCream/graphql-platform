using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    public static IType InnerNullableType(this IType type)
    {
        return type switch
        {
            ListType listType => listType.ElementType.NullableType(),
            NonNullType nonNullType => nonNullType.NullableType,
            _ => type
        };
    }
}
