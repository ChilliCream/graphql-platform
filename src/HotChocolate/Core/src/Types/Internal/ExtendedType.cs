using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal;

/// <summary>
/// The extended type provides addition type information about the underlying system type.
/// </summary>
public sealed partial class ExtendedType : IExtendedType
{
    internal static ImmutableHashSet<Type> NonEssentialWrapperTypes { get; set; } =
        [typeof(ValueTask<>), typeof(Task<>), typeof(NativeType<>), typeof(Optional<>)];

    private ExtendedType(
        Type type,
        ExtendedTypeKind kind,
        IReadOnlyList<ExtendedType>? typeArguments = null,
        Type? source = null,
        Type? definition = null,
        ExtendedType? elementType = null,
        bool isList = false,
        bool isNamedType = false,
        bool isNullable = false)
    {
        Type = type;
        Kind = kind;
        TypeArguments = typeArguments ?? Array.Empty<ExtendedType>();
        Source = source ?? type;
        Definition = definition;
        ElementType = elementType;
        IsList = isList;
        IsNamedType = isNamedType;
        IsNullable = isNullable;

        if (type.IsGenericType && definition is null)
        {
            Definition = type.GetGenericTypeDefinition();
        }

        Id = Helper.CreateIdentifier(this);
    }

    internal ExtendedTypeId Id { get; }

    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    public Type Source { get; }

    /// <inheritdoc />
    public Type? Definition { get; }

    /// <inheritdoc />
    public ExtendedTypeKind Kind { get; }

    /// <inheritdoc />
    public bool IsGeneric => Type.IsGenericType;

    /// <inheritdoc />
    public bool IsArray => Type.IsArray;

    /// <inheritdoc />
    public bool IsList { get; }

    /// <inheritdoc />
    public bool IsArrayOrList => IsList || IsArray;

    /// <inheritdoc />
    public bool IsNamedType { get; }

    /// <inheritdoc />
    public bool IsSchemaType => Kind == ExtendedTypeKind.Schema;

    /// <inheritdoc />
    public bool IsInterface => Type.IsInterface;

    /// <inheritdoc />
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the generic type information.
    /// </summary>
    public IReadOnlyList<ExtendedType> TypeArguments { get; }

    /// <inheritdoc />
    IReadOnlyList<IExtendedType> IExtendedType.TypeArguments => TypeArguments;

    /// <summary>
    /// Gets the element type if <see cref="IsArrayOrList"/> is <c>true</c>.
    /// </summary>
    public ExtendedType? ElementType { get; }

    /// <inheritdoc />
    IExtendedType? IExtendedType.ElementType => ElementType;

    /// <inheritdoc />
    public bool Equals(IExtendedType? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        if (other.Type == Type
            && other.Kind.Equals(Kind)
            && other.IsNullable.Equals(IsNullable)
            && other.TypeArguments.Count == TypeArguments.Count)
        {
            for (var i = 0; i < other.TypeArguments.Count; i++)
            {
                if (!other.TypeArguments[i].Equals(TypeArguments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as ExtendedType);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Type.GetHashCode() * 397)
                ^ (Kind.GetHashCode() * 397)
                ^ (IsNullable.GetHashCode() * 397);

            for (var i = 0; i < TypeArguments.Count; i++)
            {
                hashCode ^= (TypeArguments[i].GetHashCode() * 397 * i);
            }

            return hashCode;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        string typeName;

        if (IsArray)
        {
            typeName = $"[{TypeArguments[0]}]";
        }
        else
        {
            if (Definition is not null)
            {
                typeName = Definition.Name.Substring(0, Definition.Name.Length - 2);
                typeName = $"{typeName}<{string.Join(", ", TypeArguments)}>";
            }
            else
            {
                typeName = Type.Name;
            }
        }

        return IsNullable ? typeName : typeName + "!";
    }

    internal static ExtendedType FromType(Type type, TypeCache cache)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (cache.TryGetType(type, out var extendedType))
        {
            return extendedType;
        }

        return FromTypeInternal(type, cache);
    }

    private static ExtendedType FromTypeInternal(Type type, TypeCache cache) =>
        Helper.IsSchemaType(type)
            ? SchemaType.FromType(type, cache)
            : SystemType.FromType(type, cache);

    internal static ExtendedType FromMember(MemberInfo member, TypeCache cache)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        if (member is Type type)
        {
            return FromType(type, cache);
        }

        return Members.FromMember(member, cache);
    }

    internal static ExtendedMethodInfo FromMethod(
        MethodInfo method,
        ParameterInfo[] parameters,
        TypeCache cache)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        if (cache is null)
        {
            throw new ArgumentNullException(nameof(cache));
        }

        return Members.FromMethod(method, parameters, cache);
    }

    /// <summary>
    /// Registers generic type definitions that represent types that must be removed
    /// before a runtime type is translated to a GraphQL type.
    /// </summary>
    /// <param name="type">
    /// The generic type definition.
    /// </param>
    /// <exception cref="ArgumentException">
    /// type is null.
    /// </exception>
    public static void RegisterNonEssentialWrapperTypes(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                "The type must be a generic type definition.",
                nameof(type));
        }

        if (NonEssentialWrapperTypes.Contains(type))
        {
            return;
        }

        NonEssentialWrapperTypes = NonEssentialWrapperTypes.Add(type);
    }
}
