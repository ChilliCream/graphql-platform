#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class DefaultTypeDiscoveryHandler : TypeDiscoveryHandler
{
    public DefaultTypeDiscoveryHandler(ITypeInspector typeInspector)
    {
        TypeInspector = typeInspector ?? throw new ArgumentNullException(nameof(typeInspector));
    }

    private ITypeInspector TypeInspector { get; }

    public override bool TryInferType(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeInfo,
        [NotNullWhen(true)] out ITypeReference[]? schemaTypeRefs)
    {
        ExtendedTypeReference? schemaType;

        if (IsObjectTypeExtension(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(ObjectTypeExtension<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsUnionType(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(UnionType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsInterfaceType(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(InterfaceType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsObjectType(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(ObjectType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsInputObjectType(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(InputObjectType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsEnumType(typeInfo))
        {
            schemaType = typeReference.With(
                TypeInspector.GetType(
                    typeof(EnumType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else
        {
            schemaTypeRefs = null;
            return false;
        }

        schemaTypeRefs = new ITypeReference[] { schemaType };
        return true;
    }

    public override bool TryInferKind(
        ExtendedTypeReference typeReference,
        TypeDiscoveryInfo typeReferenceInfo,
        out TypeKind typeKind)
    {
        if (IsObjectTypeExtension(typeReferenceInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsUnionType(typeReferenceInfo))
        {
            typeKind = TypeKind.Union;
            return true;
        }

        if (IsInterfaceType(typeReferenceInfo))
        {
            typeKind = TypeKind.Interface;
            return true;
        }

        if (IsObjectType(typeReferenceInfo))
        {
            typeKind = TypeKind.Object;
            return true;
        }

        if (IsInputObjectType(typeReferenceInfo))
        {
            typeKind = TypeKind.InputObject;
            return true;
        }

        if (IsEnumType(typeReferenceInfo))
        {
            typeKind = TypeKind.Enum;
            return true;
        }

        typeKind = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsObjectTypeExtension(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnionType(TypeDiscoveryInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Union, IsTypeExtension: false } &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInterfaceType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Interface, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsInterface) &&
            typeInfo is { Context: TypeContext.Output or TypeContext.None };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInputObjectType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.InputObject, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
            typeInfo is { IsAbstract: false, Context: TypeContext.Input };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnumType(TypeDiscoveryInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Enum, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsEnum) &&
            typeInfo.IsPublic;
}
