namespace HotChocolate.Types.Extensions;

public static class CompositeTypeExtensions
{
    public static IReadOnlyNamedTypeDefinition NamedType(this IReadOnlyTypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.NonNull:
            case TypeKind.List:
                return NamedType(((IReadOnlyWrapperType)type).Type);

            case TypeKind.Object:
            case TypeKind.Interface:
            case TypeKind.Union:
            case TypeKind.InputObject:
            case TypeKind.Enum:
            case TypeKind.Scalar:
                return (IReadOnlyNamedTypeDefinition)type;

            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
