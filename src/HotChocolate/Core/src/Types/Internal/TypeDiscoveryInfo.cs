using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Internal;

/// <summary>
/// Provides addition metadata about a <see cref="ExtendedTypeReference"/>.
/// </summary>
public readonly ref struct TypeDiscoveryInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="TypeDiscoveryInfo"/>.
    /// </summary>
    public TypeDiscoveryInfo(TypeReference typeReference)
    {
        ArgumentNullException.ThrowIfNull(typeReference);

        IExtendedType extendedType;

        switch (typeReference)
        {
            case ExtendedTypeReference extendedTypeRef:
                extendedType = extendedTypeRef.Type;
                IsDirectiveRef = false;
                break;

            case ExtendedTypeDirectiveReference extendedDirectiveRef:
                extendedType = extendedDirectiveRef.Type;
                IsDirectiveRef = true;
                break;

            default:
                throw new NotSupportedException(
                    TypeDiscoveryInfo_TypeRefKindNotSupported);
        }

        ExtendedRuntimeType = extendedType;

        var attributes = extendedType.Type.Attributes;
        IsPublic = IsPublicInternal(extendedType.Type);
        IsComplex = IsComplexTypeInternal(extendedType, IsPublic);
        IsInterface = (attributes & TypeAttributes.Interface) == TypeAttributes.Interface;
        IsAbstract = (attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract;
        IsStatic = IsAbstract && (attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed;
        IsEnum = extendedType.Type.IsEnum;
        IsKeyValuePair = extendedType.IsGeneric && extendedType.Definition == typeof(KeyValuePair<,>);
        Attribute = GetTypeAttributeInternal(extendedType.Type);
        Context = typeReference.Context;
    }

    public IExtendedType ExtendedRuntimeType { get; }

    /// <summary>
    /// Gets the runtime type.
    /// </summary>
    public Type RuntimeType => ExtendedRuntimeType.Type;

    /// <summary>
    /// The type attribute if one was annotated to the <see cref="RuntimeType"/>.
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
    /// Specifies if the reference is a directive reference.
    /// </summary>
    public bool IsDirectiveRef { get; }

    /// <summary>
    /// Specifies if the <see cref="RuntimeType"/> is a <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    public bool IsKeyValuePair { get; }

    /// <summary>
    /// Specifies the <see cref="TypeContext"/> of the type reference.
    /// </summary>
    public TypeContext Context { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeAttribute? GetTypeAttributeInternal(
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
        IExtendedType unresolvedType,
        bool isPublic)
    {
        var isComplexClass =
            isPublic
            && unresolvedType.Type.IsClass
            && unresolvedType.Type != typeof(string);

        var isComplexValueType =
            isPublic
            && unresolvedType.Type is
            {
                IsValueType: true,
                IsPrimitive: false,
                IsEnum: false,
                IsByRefLike: false
            };

        return isComplexClass || isComplexValueType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPublicInternal(Type runtimeType)
        => runtimeType.IsPublic || runtimeType.IsNestedPublic;
}
