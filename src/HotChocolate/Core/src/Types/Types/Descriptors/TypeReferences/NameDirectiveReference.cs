#nullable enable
using System;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors;

public sealed class NameDirectiveReference
    : TypeReference
    , IEquatable<NameDirectiveReference>
{
    internal NameDirectiveReference(string directiveName)
        : base(TypeReferenceKind.DirectiveName, TypeContext.None, null)
    {
        Name = directiveName;
    }

    /// <summary>
    /// Gets the name of the named type.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public bool Equals(NameDirectiveReference? other)
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

        return Name.EqualsOrdinal(other.Name);
    }

    /// <inheritdoc />
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

        if (other is NameDirectiveReference c)
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

        if (obj is SyntaxTypeReference c)
        {
            return Equals(c);
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Name);

    /// <inheritdoc />
    public override string ToString()
        => $"Directive: @{Name}";
}
