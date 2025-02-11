using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Extensions;

internal static class TypeDefinitionExtensions
{
    public static IType InnerNullableType(this IType type)
    {
        return type switch
        {
            IListType listType => listType.ElementType.NullableType(),
            INonNullType nonNullType => nonNullType.NullableType,
            _ => type
        };
    }
}
