#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        var attributes = RuntimeType.Attributes;
        IsPublic = IsPublicInternal(RuntimeType);
        IsComplex = IsComplexTypeInternal(typeReference, IsPublic);
        IsInterface = (attributes & TypeAttributes.Interface) == TypeAttributes.Interface;
        IsAbstract = (attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        IsStatic = IsAbstract && (attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
        IsEnum = RuntimeType.IsEnum;
        Attribute = GetTypeAttributeInternal(typeReference, RuntimeType);
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
    /// Specifies if the <see cref="RuntimeType"/> is static.
    /// </summary>
    public bool IsStatic { get; }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeAttribute? GetTypeAttributeInternal(
        ExtendedTypeReference unresolvedType,
        Type runtimeType)
    {
        foreach (var attr in runtimeType.GetCustomAttributes(typeof(DescriptorAttribute), false))
        {
            if (attr is ITypeAttribute typeAttribute)
            {
                return typeAttribute;
            }
        }

        foreach (var attr in runtimeType.GetCustomAttributes(typeof(DescriptorAttribute), true))
        {
            if (attr is ITypeAttribute { Inherited: true } typeAttribute)
            {
                return typeAttribute;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsComplexTypeInternal(
        ExtendedTypeReference unresolvedType,
        bool isPublic)
    {
        var isComplexType =
            isPublic &&
            unresolvedType.Type.Type.IsClass &&
            unresolvedType.Type.Type != typeof(string);

        if (!isComplexType && unresolvedType.Type.IsGeneric)
        {
            var typeDefinition = unresolvedType.Type.Definition;
            return typeDefinition == typeof(KeyValuePair<,>);
        }

        return isComplexType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPublicInternal(Type runtimeType)
        => runtimeType.IsPublic || runtimeType.IsNestedPublic;
}
