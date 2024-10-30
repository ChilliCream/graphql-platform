#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class DefaultTypeDiscoveryHandler(ITypeInspector typeInspector) : TypeDiscoveryHandler
{
    private ITypeInspector TypeInspector { get; } =
        typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));

    public override bool TryInferType(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out TypeReference[]? schemaTypeRefs)
    {
        TypeReference? schemaType;

        if (typeInfo.IsStatic)
        {
            if (IsStaticObjectTypeExtension(typeInfo))
            {
                var typeExtension = new StaticObjectTypeExtension(typeInfo.RuntimeType);
                schemaType = TypeReference.Create(typeExtension, typeReference.Scope);
            }
            else
            {
                // we only allow static classes for object type extensions,
                // which are already handled above. All other static types
                // cannot be inferred.
                schemaTypeRefs = null;
                return false;
            }
        }
        else if (IsObjectTypeExtension(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(ObjectTypeExtension<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsUnionType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(UnionType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsInterfaceType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(InterfaceType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsObjectType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(ObjectType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsInputObjectType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(InputObjectType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsEnumType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(EnumType<>),
                    typeInfo,
                    typeReference);
        }
        else if (IsDirectiveType(typeInfo))
        {
            schemaType =
                TypeInspector.CreateTypeRef(
                    typeof(DirectiveType<>),
                    typeInfo,
                    typeReference);
        }
        else
        {
            schemaTypeRefs = null;
            return false;
        }

        schemaTypeRefs = [schemaType,];
        return true;
    }

    public override bool TryInferKind(
        TypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        out TypeKind typeKind)
    {
        if (IsObjectTypeExtension(typeInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsUnionType(typeInfo))
        {
            typeKind = TypeKind.Union;
            return true;
        }

        if (IsInterfaceType(typeInfo))
        {
            typeKind = TypeKind.Interface;
            return true;
        }

        if (IsObjectType(typeInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsInputObjectType(typeInfo))
        {
            typeKind = TypeKind.InputObject;
            return true;
        }

        if (IsEnumType(typeInfo))
        {
            typeKind = TypeKind.Enum;
            return true;
        }

        if (IsDirectiveType(typeInfo))
        {
            typeKind = TypeKind.Directive;
            return true;
        }

        typeKind = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsStaticObjectTypeExtension(TypeDiscoveryInfo typeInfo)
        => typeInfo.IsStatic &&
            typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectTypeExtension(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectType(TypeDiscoveryInfo typeInfo)
        => !typeInfo.IsDirectiveRef &&
            (typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: false, } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnionType(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Union, IsTypeExtension: false, } &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInterfaceType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Interface, IsTypeExtension: false, } ||
                typeInfo.Attribute is null && typeInfo.IsInterface) &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInputObjectType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.InputObject, IsTypeExtension: false, } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
            typeInfo is { IsAbstract: false, Context: TypeContext.Input, };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnumType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Enum, IsTypeExtension: false, } ||
                typeInfo.Attribute is null && typeInfo.IsEnum) &&
            typeInfo.IsPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectiveType(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Directive, IsTypeExtension: false, };
}
