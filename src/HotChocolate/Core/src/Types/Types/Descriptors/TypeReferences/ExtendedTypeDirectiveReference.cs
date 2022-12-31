#nullable enable
using System;
using HotChocolate.Internal;

namespace HotChocolate.Types.Descriptors;

public sealed class ExtendedTypeDirectiveReference
    : TypeReference
    , IEquatable<ExtendedTypeDirectiveReference>
{
    public ExtendedTypeDirectiveReference(
        IExtendedType type)
        : base(TypeReferenceKind.DirectiveExtendedType, TypeContext.None, null)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    public IExtendedType Type { get; }

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

    public override bool Equals(ITypeReference? other)
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

    public override int GetHashCode()
    {
        unchecked
        {
            return base.GetHashCode() ^ Type.GetHashCode() * 397;
        }
    }

    public override string ToString()
        => $"Directive: {Type}";
}
