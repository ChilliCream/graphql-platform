using HotChocolate.Language;

namespace HotChocolate.Skimmed;

internal static class TypeExtensions
{
    public static bool IsInputType(this IType type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => false,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.List => IsInputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsInputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsOutputType(this IType type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => true,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => false,
            TypeKind.List => IsOutputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsOutputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static ITypeNode ToTypeNode(this IType type)
    {
        switch (type)
        {
            case INamedType namedType:
                return new NamedTypeNode(namedType.Name);

            case ListType listType:
                return new ListTypeNode(ToTypeNode(listType.ElementType));

            case NonNullType nonNullType:
                return new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType));

            default:
                throw new NotSupportedException();
        }
    }
}
