#nullable enable

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
    public NameString Name { get; internal set; }

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
            Depth.Equals(name.Depth) &&
            Name.Equals(name.Name))
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
        => new NamePathSegment { Depth = Depth, Name = Name, Parent = Parent.Clone() };

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (Parent?.GetHashCode() ?? 0) * 3;
            hash ^= Depth.GetHashCode() * 7;
            hash ^= Name.GetHashCode() * 11;
            return hash;
        }
    }
}
