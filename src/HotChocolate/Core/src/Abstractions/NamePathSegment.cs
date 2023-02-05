#nullable enable

using System;
using static System.StringComparison;

namespace HotChocolate;

/// <summary>
/// An <see cref="IndexerPathSegment" /> represents a pointer to
/// an named element in the result structure.
/// </summary>
public sealed class NamePathSegment : Path
{
    /// <summary>
    ///  Gets the name representing a field on a result map.
    /// </summary>
    public string Name { get; internal set; } = default!;

    /// <inheritdoc />
    public override string Print()
    {
        var parent = Parent.IsRoot
            ? string.Empty
            : Parent.Print();
        return $"{parent}/{Name}";
    }

    /// <inheritdoc />
    public override bool Equals(Path? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (other is NamePathSegment name &&
            Length.Equals(name.Length) &&
            string.Equals(Name, name.Name, Ordinal))
        {
            if (ReferenceEquals(Parent, other.Parent))
            {
                return true;
            }

            return Parent.Equals(name.Parent);
        }

        return false;
    }

    /// <inheritdoc />
    public override Path Clone()
        => new NamePathSegment { Length = Length, Name = Name, Parent = Parent.Clone() };

    /// <inheritdoc />
    public override int GetHashCode()
        // ReSharper disable NonReadonlyMemberInGetHashCode
        => HashCode.Combine(Parent, Length, Name);
        // ReSharper restore NonReadonlyMemberInGetHashCode
}
