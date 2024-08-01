using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Represent a type reference that points to a concrete GraphQL type instance.
/// </summary>
public sealed class SchemaTypeReference
    : TypeReference
    , IEquatable<SchemaTypeReference>
{
    internal SchemaTypeReference(
        ITypeSystemMember type,
        TypeContext? context = null,
        string? scope = null)
        : base(TypeReferenceKind.SchemaType, context ?? InferTypeContext(type), scope)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// The GraphQL type instance.
    /// </summary>
    public ITypeSystemMember Type { get; }

    /// <inheritdoc />
    public bool Equals(SchemaTypeReference? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!IsEqual(other))
        {
            return false;
        }

        return Type.Equals(other.Type);
    }

    /// <inheritdoc />
    public override bool Equals(TypeReference? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is SchemaTypeReference str)
        {
            return Equals(str);
        }

        return false;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is SchemaTypeReference str)
        {
            return Equals(str);
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return base.GetHashCode() ^ Type.GetHashCode() * 397;
        }
    }

    /// <inheritdoc />
    public override string ToString()
        => ToString(Type);

    public SchemaTypeReference WithType(ITypeSystemMember type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new SchemaTypeReference(type, Context, Scope);
    }

    public SchemaTypeReference WithContext(TypeContext context = TypeContext.None)
    {
        return new SchemaTypeReference(Type, context, Scope);
    }

    public SchemaTypeReference WithScope(string? scope = null)
    {
        return new SchemaTypeReference(Type, Context, scope);
    }

    public SchemaTypeReference With(
        Optional<ITypeSystemMember> type = default,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default)
    {
        if (type.HasValue && type.Value is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new SchemaTypeReference(
            type.HasValue ? type.Value! : Type,
            context.HasValue ? context.Value : Context,
            scope.HasValue ? scope.Value : Scope);
    }

    internal static TypeContext InferTypeContext(object? type)
    {
        if (type is IType t)
        {
            var namedType = t.NamedType();

            if (namedType.IsInputType() && namedType.IsOutputType())
            {
                return TypeContext.None;
            }

            if (namedType.IsOutputType())
            {
                return TypeContext.Output;
            }

            if (namedType.IsInputType())
            {
                return TypeContext.Input;
            }
        }

        if (type is Type ts)
        {
            return InferTypeContext(ts);
        }

        return TypeContext.None;
    }

    internal static TypeContext InferTypeContext(IExtendedType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return InferTypeContext(type.Type);
    }

    internal static TypeContext InferTypeContext(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var namedType = ExtendedType.Tools.GetNamedType(type);
        return InferTypeContextInternal(namedType ?? type);
    }

    private static TypeContext InferTypeContextInternal(Type type)
    {
        if (typeof(IInputType).IsAssignableFrom(type)
            && typeof(IOutputType).IsAssignableFrom(type))
        {
            return TypeContext.None;
        }

        if (typeof(IOutputType).IsAssignableFrom(type))
        {
            return TypeContext.Output;
        }

        if (typeof(IInputType).IsAssignableFrom(type))
        {
            return TypeContext.Input;
        }

        return TypeContext.None;
    }
}
