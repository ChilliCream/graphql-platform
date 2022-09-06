using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration;

public static class SchemaTypeResolver
{
    private static readonly Type _keyValuePair = typeof(KeyValuePair<,>);

    public static bool TryInferSchemaType(
        ITypeInspector typeInspector,
        ExtendedTypeReference unresolvedType,
        [NotNullWhen(true)] out ExtendedTypeReference? schemaType)
    {
        if (typeInspector is null)
        {
            throw new ArgumentNullException(nameof(typeInspector));
        }

        if (unresolvedType is null)
        {
            throw new ArgumentNullException(nameof(unresolvedType));
        }

        var typeInfo = new TypeInfo(unresolvedType);

        if (IsObjectTypeExtension(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(ObjectTypeExtension<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsUnionType(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(UnionType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsInterfaceType(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(InterfaceType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsObjectType(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(ObjectType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsInputObjectType(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(InputObjectType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (IsEnumType(typeInfo))
        {
            schemaType = unresolvedType.With(
                typeInspector.GetType(
                    typeof(EnumType<>).MakeGenericType(typeInfo.RuntimeType)));
        }
        else if (Scalars.TryGetScalar(unresolvedType.Type.Type, out var scalarType))
        {
            schemaType = unresolvedType.With(typeInspector.GetType(scalarType));
        }
        else
        {
            schemaType = null;
        }

        return schemaType != null;
    }

    public static bool TryInferSchemaTypeKind(
        ExtendedTypeReference unresolvedType,
        out TypeKind kind)
    {
        if (unresolvedType == null)
        {
            throw new ArgumentNullException(nameof(unresolvedType));
        }

        var typeInfo = new TypeInfo(unresolvedType);

        if (IsObjectTypeExtension(typeInfo))
        {
            kind = TypeKind.Object;
            return true;
        }

        if (IsUnionType(typeInfo))
        {
            kind = TypeKind.Union;
            return true;
        }

        if (IsInterfaceType(typeInfo))
        {
            kind = TypeKind.Interface;
            return true;
        }

        if (IsObjectType(typeInfo))
        {
            kind = TypeKind.Object;
            return true;
        }

        if (IsInputObjectType(typeInfo))
        {
            kind = TypeKind.InputObject;
            return true;
        }

        if (IsEnumType(typeInfo))
        {
            kind = TypeKind.Enum;
            return true;
        }

        if (Scalars.TryGetScalar(unresolvedType.Type.Type, out _))
        {
            kind = TypeKind.Scalar;
            return true;
        }

        kind = default;
        return false;
    }

    private static bool IsObjectType(TypeInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
           typeInfo is { Context: TypeContext.Output or TypeContext.None };

    private static bool IsObjectTypeExtension(TypeInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Object, IsTypeExtension: true };

    private static bool IsUnionType(TypeInfo typeInfo)
        => typeInfo.Attribute is { Kind: TypeKind.Union, IsTypeExtension: false } &&
           typeInfo is { Context: TypeContext.Output or TypeContext.None };

    private static bool IsInterfaceType(TypeInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Interface, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsInterface) &&
           typeInfo is { Context: TypeContext.Output or TypeContext.None };

    private static bool IsInputObjectType(TypeInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.InputObject, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsComplex) &&
           typeInfo is { IsAbstract: false, Context: TypeContext.Input };

    private static bool IsEnumType(TypeInfo typeInfo)
        => (typeInfo.Attribute is { Kind: TypeKind.Enum, IsTypeExtension: false } ||
                typeInfo.Attribute is null && typeInfo.IsEnum) &&
           typeInfo.IsPublic;

    private readonly ref struct TypeInfo
    {
        public TypeInfo(ExtendedTypeReference typeReference)
        {
            RuntimeType = typeReference.Type.Type;
            IsPublic = IsPublicInternal(typeReference);
            IsComplex = IsComplexTypeInternal(typeReference, IsPublic);
            IsInterface = RuntimeType.IsInterface;
            IsAbstract = RuntimeType.IsAbstract;
            IsEnum = RuntimeType.IsEnum;
            Attribute = GetTypeAttributeInternal(typeReference);
            Context = typeReference.Context;
        }

        public Type RuntimeType { get; }

        public ITypeAttribute? Attribute { get; }

        public bool IsInterface { get; }

        public bool IsComplex { get; }

        public bool IsAbstract { get; }

        public bool IsEnum { get; }

        public bool IsPublic { get; }

        public TypeContext Context { get; }

        private static ITypeAttribute? GetTypeAttributeInternal(
            ExtendedTypeReference unresolvedType)
        {
            var runtimeType = unresolvedType.Type.Type;

            foreach (var attribute in
                runtimeType.GetCustomAttributes(typeof(DescriptorAttribute), false))
            {
                if (attribute is ITypeAttribute typeAttribute)
                {
                    return typeAttribute;
                }
            }

            foreach (var attribute in
                runtimeType.GetCustomAttributes(typeof(DescriptorAttribute), true))
            {
                if (attribute is ITypeAttribute { Inherited: true } typeAttribute)
                {
                    return typeAttribute;
                }
            }

            return null;
        }

        private static bool IsComplexTypeInternal(
            ExtendedTypeReference unresolvedType,
            bool isPublic)
        {
            var isComplexType =
                unresolvedType.Type.Type.IsClass
                && isPublic
                && unresolvedType.Type.Type != typeof(string);

            if (!isComplexType && unresolvedType.Type.IsGeneric)
            {
                var typeDefinition = unresolvedType.Type.Definition;
                return typeDefinition == _keyValuePair;
            }

            return isComplexType;
        }

        private static bool IsPublicInternal(ExtendedTypeReference unresolvedType)
            => unresolvedType.Type.Type.IsPublic ||
               unresolvedType.Type.Type.IsNestedPublic;
    }
}
