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
    /// <summary>
    /// Initializes a new instance of <see cref="ExtendedTypeReference"/>.
    /// </summary>
    /// <param name="type">
    /// The extended type.
    /// </param>
    /// <param name="context">
    /// The type context.
    /// </param>
    /// <param name="scope">
    /// The type scope.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
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
        => HashCode.Combine(base.GetHashCode(), Type.GetHashCode());

    /// <inheritdoc />
    public override string ToString()
        => ToString(Type);

    /// <summary>
    /// Creates a new <see cref="ExtendedTypeReference"/> and
    /// replaces the <see cref="Type"/> with the provided <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The extended type.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ExtendedTypeReference"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/> is <c>null</c>.
    /// </exception>
    public ExtendedTypeReference WithType(IExtendedType type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Equals(Type))
        {
            return this;
        }

        return Create(type, Context, Scope);
    }

    /// <summary>
    /// Creates a new <see cref="ExtendedTypeReference"/> and replaces the
    /// <see cref="TypeReference.Context"/> with the provided <paramref name="context"/>.
    /// </summary>
    /// <param name="context">
    /// The type context.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ExtendedTypeReference"/>.
    /// </returns>
    public ExtendedTypeReference WithContext(TypeContext context = TypeContext.None)
    {
        if (context == Context)
        {
            return this;
        }

        return Create(
            Type,
            context,
            Scope);
    }

    /// <summary>
    /// Creates a new <see cref="ExtendedTypeReference"/> and replaces the
    /// <see cref="TypeReference.Scope"/> with the provided
    /// <paramref name="scope"/>.
    /// </summary>
    /// <param name="scope">
    /// The type scope.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ExtendedTypeReference"/>.
    /// </returns>
    public ExtendedTypeReference WithScope(string? scope = null)
    {
        if (string.Equals(scope, Scope, StringComparison.Ordinal))
        {
            return this;
        }

        return Create(
            Type,
            Context,
            scope);
    }

    /// <summary>
    /// Creates a new <see cref="ExtendedTypeReference"/> and allows to replace certain aspects
    /// of the original instance.
    /// </summary>
    /// <param name="type">
    /// The extended type.
    /// </param>
    /// <param name="context">
    /// The type context.
    /// </param>
    /// <param name="scope">
    /// The type scope.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ExtendedTypeReference"/>.
    /// </returns>
    public ExtendedTypeReference With(
        IExtendedType? type = default,
        Optional<TypeContext> context = default,
        Optional<string?> scope = default)
        => Create(
            type ?? Type,
            context.HasValue
                ? context.Value
                : Context,
            scope.HasValue
                ? scope.Value
                : Scope);
}
