using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

public static class TypeExtensions
{
    public static bool IsListType(this IType type)
        => type.Kind switch
        {
            TypeKind.List => true,
            TypeKind.NonNull when ((NonNullType)type).NullableType.Kind == TypeKind.List => true,
            _ => false,
        };

    public static bool IsInputType(this IType type)
        => type.Kind switch
        {
            TypeKind.InputObject or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.Interface or TypeKind.Object or TypeKind.Union => false,
            TypeKind.List => IsInputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsInputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsOutputType(this IType type)
        => type.Kind switch
        {
            TypeKind.Interface or TypeKind.Object or TypeKind.Union or TypeKind.Enum or TypeKind.Scalar => true,
            TypeKind.InputObject => false,
            TypeKind.List => IsOutputType(((ListType)type).ElementType),
            TypeKind.NonNull => IsOutputType(((NonNullType)type).NullableType),
            _ => throw new NotSupportedException(),
        };

    public static bool IsTypeExtension(this IType type)
        => type is IFeatureProvider featureProvider
            && featureProvider.Features.Get<TypeMetadata>() is { IsExtension: true };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IType InnerType(this IType type)
        => type switch
        {
            ListType listType => listType.ElementType,
            NonNullType nonNullType => nonNullType.NullableType,
            _ => type,
        };

    public static IType NullableType(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.Kind == TypeKind.NonNull
            ? ((NonNullType)type).NullableType
            : type;
    }

    public static ITypeNode ToTypeNode(this IType type)
        => type switch
        {
            ITypeDefinition namedType => new NamedTypeNode(namedType.Name),
            ListType listType => new ListTypeNode(ToTypeNode(listType.ElementType)),
            NonNullType nonNullType => new NonNullTypeNode((INullableTypeNode)ToTypeNode(nonNullType.NullableType)),
            _ => throw new NotSupportedException(),
        };

    public static IType ReplaceNameType(this IType type, Func<string, ITypeDefinition> newNamedType)
        => type switch
        {
            ITypeDefinition namedType => newNamedType(namedType.Name),
            ListType listType => new ListType(ReplaceNameType(listType.ElementType, newNamedType)),
            NonNullType nonNullType => new NonNullType(ReplaceNameType(nonNullType.NullableType, newNamedType)),
            _ => throw new NotSupportedException(),
        };
}
