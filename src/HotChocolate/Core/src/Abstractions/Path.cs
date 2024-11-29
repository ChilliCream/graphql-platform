using System.Text;

namespace HotChocolate;

/// <summary>
/// An <see cref="Path" /> represents a pointer to an element in the result structure.
/// </summary>
public abstract class Path : IEquatable<Path>
{
    private readonly Path? _parent;

    protected Path()
    {
        _parent = null;
        Length = 0;
    }

    protected Path(Path parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        Length = parent.Length + 1;
    }

    /// <summary>
    /// Gets the parent path segment.
    /// </summary>
    public Path Parent => _parent!;

    /// <summary>
    /// Gets the count of segments this path contains.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Returns true if the Path is the root element
    /// </summary>
    public bool IsRoot => ReferenceEquals(this, Root);

    public Path Append(string name)
        => new NamePathSegment(this, name);

    /// <summary>
    /// Appends an indexer to this path and returns the new path segment.
    /// </summary>
    /// <param name="index">
    /// The index.
    /// </param>
    /// <returns>
    /// Returns the new path segment.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Appending a indexer on the root segment is not allowed.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The index must be greater than or equal to zero.
    /// </exception>
    public Path Append(int index)
    {
        if (this is RootPathSegment)
        {
            throw new InvalidOperationException(
                "Appending a indexer on the root segment is not allowed.");
        }

        return new IndexerPathSegment(this, index);
    }

    /// <summary>
    /// Generates a string that represents the current path.
    /// </summary>
    /// <returns>
    /// Returns a string that represents the current path.
    /// </returns>
    public string Print()
    {
        if (this is RootPathSegment)
        {
            return "/";
        }

        var sb = new StringBuilder();
        var current = this;

        while (current is not RootPathSegment)
        {
            switch (current)
            {
                case IndexerPathSegment indexer:
                    var numberValue = indexer.Index.ToString();
                    sb.Insert(0, '[');
                    sb.Insert(1, numberValue);
                    sb.Insert(1 + numberValue.Length, ']');
                    break;

                case NamePathSegment name:
                    sb.Insert(0, '/');
                    sb.Insert(1, name.Name);
                    break;

                default:
                    throw new NotSupportedException();
            }

            current = current.Parent;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a new list representing the current <see cref="Path"/>.
    /// </summary>
    /// <returns>
    /// Returns a new list representing the current <see cref="Path"/>.
    /// </returns>
    public IReadOnlyList<object> ToList()
    {
        if (IsRoot)
        {
            return Array.Empty<object>();
        }

        var stack = new List<object>();
        var current = this;

        while (!current.IsRoot)
        {
            switch (current)
            {
                case IndexerPathSegment indexer:
                    stack.Insert(0, indexer.Index);
                    break;

                case NamePathSegment name:
                    stack.Insert(0, name.Name);
                    break;

                default:
                    throw new NotSupportedException();
            }

            current = current.Parent;
        }

        return stack;
    }

    /// <summary>Returns a string that represents the current <see cref="Path"/>.</summary>
    /// <returns>A string that represents the current <see cref="Path"/>.</returns>
    public override string ToString() => Print();

    public virtual bool Equals(Path? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (Length.Equals(other.Length) &&
            Parent.Equals(other.Parent))
        {
            return true;
        }

        return false;
    }

    public sealed override bool Equals(object? obj)
        => obj switch
        {
            null => false,
            Path p => Equals(p),
            _ => false,
        };

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="Path"/>.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(Parent, Length);

    internal static Path FromList(params object[] elements)
        => FromList((IReadOnlyList<object>)elements);

    internal static Path FromList(IReadOnlyList<object> path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.Count == 0)
        {
            return Root;
        }

        var segment = Root;

        for (var i = 0; i < path.Count; i++)
        {
            segment = path[i] switch
            {
                string n => segment.Append(n),
                int n => segment.Append(n),
                _ => throw new NotSupportedException(),
            };
        }

        return segment;
    }

    public static Path Root => RootPathSegment.Instance;

    private sealed class RootPathSegment : Path
    {
        private RootPathSegment()
        {
        }

        /// <inheritdoc />
        public override bool Equals(Path? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return ReferenceEquals(other, this);
        }

        /// <inheritdoc />
        public override int GetHashCode()
            => 0;

        public static RootPathSegment Instance { get; } = new();
    }
}
