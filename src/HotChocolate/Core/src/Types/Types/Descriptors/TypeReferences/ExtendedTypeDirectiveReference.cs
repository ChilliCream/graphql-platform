#nullable enable
using HotChocolate.Internal;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// Represents a reference to a directive by its runtime type.
/// </summary>
public sealed class ExtendedTypeDirectiveReference
    : TypeReference
    , IEquatable<ExtendedTypeDirectiveReference>
{
    internal ExtendedTypeDirectiveReference(
        IExtendedType type)
        : base(TypeReferenceKind.DirectiveExtendedType, TypeContext.None, null)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// The runtime type.
    /// </summary>
    public IExtendedType Type { get; }

    /// <inheritdoc />
    public bool Equals(ExtendedTypeDirectiveReference? other)
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

        if (other is ExtendedTypeDirectiveReference c)
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

        if (obj is ExtendedTypeDirectiveReference c)
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
        => $"@{Type}";
}
