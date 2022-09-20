#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

public readonly ref struct TypeDiscoveryInfo
{
    public TypeDiscoveryInfo(ExtendedTypeReference typeReference)
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
            unresolvedType.Type.Type.IsClass &&
            isPublic &&
            unresolvedType.Type.Type != typeof(string);

        if (!isComplexType && unresolvedType.Type.IsGeneric)
        {
            var typeDefinition = unresolvedType.Type.Definition;
            return typeDefinition == typeof(KeyValuePair<,>);
        }

        return isComplexType;
    }

    private static bool IsPublicInternal(ExtendedTypeReference unresolvedType)
        => unresolvedType.Type.Type.IsPublic ||
            unresolvedType.Type.Type.IsNestedPublic;
}
