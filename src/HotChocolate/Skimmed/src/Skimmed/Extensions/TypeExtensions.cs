using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public static class TypeExtensions
{
    public static bool IsListType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.List => true,
            TypeKind.NonNull when ((NonNullType)type).NullableType.Kind == TypeKind.List => true,
            _ => false,
        };

    public static bool IsInputType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => false,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.List => IsInputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsInputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsOutputType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => true,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => false,
            TypeKind.List => IsOutputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsOutputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ITypeDefinition InnerType(this ITypeDefinition type)
        => type switch
        {
            ListType listType => listType.ElementType,
            NonNullType nonNullType => nonNullType.NullableType,
            _ => type,
        };

    public static INamedTypeDefinition NamedType(this ITypeDefinition type)
    {
        while (true)
        {
            switch (type)
            {
                case INamedTypeDefinition namedType:
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

    public static ITypeNode ToTypeNode(this ITypeDefinition type)
        => type switch
        {
            INamedTypeDefinition namedType => new NamedTypeNode(namedType.Name),
            ListType listType => new ListTypeNode(ToTypeNode(listType.ElementType)),
            NonNullType nonNullType => new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType)),
            _ => throw new NotSupportedException(),
        };

    public static ITypeDefinition ReplaceNameType(this ITypeDefinition type, Func<string, INamedTypeDefinition> newNamedType)
        => type switch
        {
            INamedTypeDefinition namedType => newNamedType(namedType.Name),
            ListType listType => new ListType(ReplaceNameType(listType.ElementType, newNamedType)),
            NonNullType nonNullType => new NonNullType(ReplaceNameType(nonNullType.NullableType, newNamedType)),
            _ => throw new NotSupportedException(),
        };
}
