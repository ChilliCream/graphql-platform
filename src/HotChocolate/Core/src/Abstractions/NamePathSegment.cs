using HotChocolate.Utilities;
using static System.StringComparison;

namespace HotChocolate;

/// <summary>
/// An <see cref="IndexerPathSegment" /> represents a pointer to
/// an named element in the result structure.
/// </summary>
public sealed class NamePathSegment : Path
{
    internal NamePathSegment(Path parent, string name) : base(parent)
    {
        name.EnsureGraphQLName();
        Name = name;
    }

    /// <summary>
    ///  Gets the name representing a field on a result map.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override bool Equals(Path? other)
        => base.Equals(other) &&
            other is NamePathSegment name &&
            Name.Equals(name.Name, Ordinal);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Name);
}
