using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public static class TypeExtensions
{
    public static bool IsListType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.List => true,
            TypeKind.NonNull when ((NonNullTypeDefinition)type).NullableType.Kind == TypeKind.List => true,
            _ => false,
        };

    public static bool IsInputType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => false,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.List => IsInputType(((ListTypeDefinition)type).ElementType),
            TypeKind.NonNull => IsInputType(((NonNullTypeDefinition)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsOutputType(this ITypeDefinition type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => true,
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => false,
            TypeKind.List => IsOutputType(((ListTypeDefinition)type).ElementType),
            TypeKind.NonNull => IsOutputType(((NonNullTypeDefinition)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsTypeExtension(this ITypeDefinition type)
        => type is IFeatureProvider featureProvider
            && featureProvider.Features.Get<TypeMetadata>() is { IsExtension: true };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ITypeDefinition InnerType(this ITypeDefinition type)
        => type switch
        {
            ListTypeDefinition listType => listType.ElementType,
            NonNullTypeDefinition nonNullType => nonNullType.NullableType,
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

                case ListTypeDefinition listType:
                    type = listType.ElementType;
                    continue;

                case NonNullTypeDefinition nonNullType:
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
            ListTypeDefinition listType => new ListTypeNode(ToTypeNode(listType.ElementType)),
            NonNullTypeDefinition nonNullType => new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType)),
            _ => throw new NotSupportedException(),
        };

    public static ITypeDefinition ReplaceNameType(this ITypeDefinition type, Func<string, INamedTypeDefinition> newNamedType)
        => type switch
        {
            INamedTypeDefinition namedType => newNamedType(namedType.Name),
            ListTypeDefinition listType => new ListTypeDefinition(ReplaceNameType(listType.ElementType, newNamedType)),
            NonNullTypeDefinition nonNullType => new NonNullTypeDefinition(ReplaceNameType(nonNullType.NullableType, newNamedType)),
            _ => throw new NotSupportedException(),
        };
}
