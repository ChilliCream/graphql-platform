using HotChocolate.Language;

namespace HotChocolate.Skimmed;

public static class TypeExtensions
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

    public static IType InnerType(this IType type)
    {
        switch (type)
        {
            case ListType listType:
                return listType.ElementType;


            case NonNullType nonNullType:
                return nonNullType.NullableType;

            default:
                return type;
        }
    }

    public static INamedType NamedType(this IType type)
    {
        while (true)
        {
            switch (type)
            {
                case INamedType namedType:
                    return namedType;

                case ListType listType:
                    type = listType.ElementType;
                    continue;

                case NonNullType nonNullType:
                    type = nonNullType.NullableType;
                    continue;

                default:
                    throw new NotSupportedException();
            }
        }
    }

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

    public static IType ReplaceNameType(this IType type, Func<string, INamedType> newNamedType)
    {
        switch (type)
        {
            case INamedType namedType:
                return newNamedType(namedType.Name);

            case ListType listType:
                return new ListType(ReplaceNameType(listType.ElementType, newNamedType));

            case NonNullType nonNullType:
                return new NonNullType(ReplaceNameType(nonNullType.NullableType, newNamedType));

            default:
                throw new NotSupportedException();
        }
    }
}
