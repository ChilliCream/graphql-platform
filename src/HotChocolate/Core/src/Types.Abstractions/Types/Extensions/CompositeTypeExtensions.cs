namespace HotChocolate.Types.Extensions;

public static class CompositeTypeExtensions
{
    public static INamedTypeDefinition NamedType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.NonNull => NamedType(((INonNullTypeDefinition)type).NullableType),
            TypeKind.List => NamedType(((IListTypeDefinition)type).ElementType),
            TypeKind.Object or
                TypeKind.Interface or
                TypeKind.Union or
                TypeKind.InputObject or
                TypeKind.Enum or
                TypeKind.Scalar => (INamedTypeDefinition)type,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}
