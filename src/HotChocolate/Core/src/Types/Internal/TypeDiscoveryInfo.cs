#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// Provides addition metadata about a <see cref="ExtendedTypeReference"/>.
/// </summary>
public readonly ref struct TypeDiscoveryInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeDiscoveryInfo"/>.
    /// </summary>
    public TypeDiscoveryInfo(ExtendedTypeReference typeReference)
    {
        if (typeReference is null)
        {
            throw new ArgumentNullException(nameof(typeReference));
        }

        RuntimeType = typeReference.Type.Type;
        IsPublic = IsPublicInternal(typeReference);
        IsComplex = IsComplexTypeInternal(typeReference, IsPublic);
        IsInterface = RuntimeType.IsInterface;
        IsAbstract = RuntimeType.IsAbstract;
        IsEnum = RuntimeType.IsEnum;
        Attribute = GetTypeAttributeInternal(typeReference);
        Context = typeReference.Context;
    }

    /// <summary>
    /// Gets the runtime type.
    /// </summary>
    public Type RuntimeType { get; }

    /// <summary>
    /// The the type attribute if one was annotated to the <see cref="RuntimeType"/>.
    /// </summary>
    public ITypeAttribute? Attribute { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is an interface.
    /// </summary>
    public bool IsInterface { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is a complex type.
    /// </summary>
    public bool IsComplex { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is abstract.
    /// </summary>
    public bool IsAbstract { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is an enum.
    /// </summary>
    public bool IsEnum { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is public.
    /// </summary>
    public bool IsPublic { get; }

    /// <summary>
    /// Specifies the <see cref="TypeContext"/> of the type reference.
    /// </summary>
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
