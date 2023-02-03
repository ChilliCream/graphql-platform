using System;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Represents a reference to a GraphQL type by its runtime type.
/// </summary>
public sealed class ExtendedTypeReference
    : TypeReference
    , IEquatable<ExtendedTypeReference>
{
    internal ExtendedTypeReference(
        IExtendedType type,
        TypeContext context,
        string? scope = null)
        : base(TypeReferenceKind.ExtendedType, context, scope)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Gets the runtime type.
    /// </summary>
    public IExtendedType Type { get; }

    /// <inheritdoc />
    public bool Equals(ExtendedTypeReference? other)
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

        return ReferenceEquals(Type, other.Type) || Type.Equals(other.Type);
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

        if (other is ExtendedTypeReference c)
        {
            return Equals(c);
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

        if (obj is ExtendedTypeReference c)
        {
            return Equals(c);
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

    public ExtendedTypeReference WithType(IExtendedType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Create(
            type,
            Context,
            Scope);
    }

    public ExtendedTypeReference WithContext(TypeContext context = TypeContext.None)
    {
        return Create(
            Type,
            context,
            Scope);
    }

    public ExtendedTypeReference WithScope(string? scope = null)
    {
        return Create(
            Type,
            Context,
            scope);
    }

    public ExtendedTypeReference With(
        IExtendedType? type = default,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default)
    {
        return Create(
            type ?? Type,
            context.HasValue ? context.Value : Context,
            scope.HasValue ? scope.Value : Scope);
    }
}
