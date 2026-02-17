using System.Text;

namespace HotChocolate;

/// <summary>
/// An <see cref="Path" /> represents a pointer to an element in the result structure.
/// </summary>
public abstract class Path : IEquatable<Path>, IComparable<Path>
{
    private readonly Path? _parent;

    protected Path()
    {
        _parent = null;
        Length = 0;
    }

    protected Path(Path parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

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
    /// Appending an indexer on the root segment is not allowed.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The index must be greater than or equal to zero.
    /// </exception>
    public Path Append(int index)
    {
        if (this is RootPathSegment)
        {
            throw new InvalidOperationException(
                "Appending an indexer on the root segment is not allowed.");
        }

        return new IndexerPathSegment(this, index);
    }

    /// <summary>
    /// Appends another path to this path.
    /// </summary>
    /// <param name="path">
    /// The other path.
    /// </param>
    /// <returns>
    /// the combined path.
    /// </returns>
    public Path Append(Path path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.IsRoot)
        {
            return this;
        }

        var stack = new Stack<object>();
        var current = path;

        while (!current.IsRoot)
        {
            switch (current)
            {
                case IndexerPathSegment indexer:
                    stack.Push(indexer.Index);
                    break;

                case NamePathSegment name:
                    stack.Push(name.Name);
                    break;

                default:
                    throw new NotSupportedException("Unsupported path segment type.");
            }

            current = current.Parent;
        }

        var newPath = this;

        while (stack.Count > 0)
        {
            var segment = stack.Pop();
            newPath = segment switch
            {
                string name => newPath.Append(name),
                int index => newPath.Append(index),
                _ => throw new NotSupportedException("Unsupported path segment type.")
            };
        }

        return newPath;
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

    /// <summary>
    /// Creates a new list representing the current <see cref="Path"/>.
    /// </summary>
    /// <returns>
    /// Returns a new list representing the current <see cref="Path"/>.
    /// </returns>
    public void ToList(Span<object> path)
    {
        if (IsRoot)
        {
            return;
        }

        if (path.Length < Length)
        {
            throw new ArgumentException(
                "The path span mustn't be smaller than the length of the path.",
                nameof(path));
        }

        var current = this;
        var length = path.Length;

        while (!current.IsRoot)
        {
            switch (current)
            {
                case IndexerPathSegment indexer:
                    path[--length] = indexer.Index;
                    break;

                case NamePathSegment name:
                    path[--length] = name.Name;
                    break;

                default:
                    throw new NotSupportedException();
            }

            current = current.Parent;
        }
    }

    public IEnumerable<Path> EnumerateSegments()
        => EnumerateSegmentsBackwards().Reverse();

    private IEnumerable<Path> EnumerateSegmentsBackwards()
    {
        if (IsRoot)
        {
            yield break;
        }

        var current = this;

        while (!current.IsRoot)
        {
            yield return current;
            current = current.Parent;
        }
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

        if (Length.Equals(other.Length) && Parent.Equals(other.Parent))
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
            _ => false
        };

    /// <summary>
    /// Compares the current instance with another object of the same type and returns
    /// </summary>
    /// <param name="other">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// A 32-bit signed integer that indicates the relative order of the objects being compared.
    /// </returns>
    public int CompareTo(Path? other)
    {
        if (other is null)
        {
            return -1;
        }

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (IsRoot)
        {
            return other.IsRoot ? 0 : -1;
        }

        if (other.IsRoot)
        {
            return 1;
        }

        // 1. Align to the same depth
        var a = this;
        var b = other;

        var lenA = a.Length;
        var lenB = b.Length;

        if (lenA > lenB)
        {
            a = a.Skip(lenA - lenB);
        }
        else if (lenB > lenA)
        {
            b = b.Skip(lenB - lenA);
        }

        // 2. Walk aligned segments from root to leaf
        var cmp = CompareFromRoot(a, b);
        if (cmp != 0)
        {
            return cmp;
        }

        // 3. Same segments → shorter path wins
        return Length.CompareTo(other.Length);
    }

    private Path Skip(int count)
    {
        var current = this;
        for (var i = 0; i < count; i++)
        {
            current = current.Parent;
        }
        return current;
    }

    private static int CompareFromRoot(Path a, Path b)
    {
        if (a.IsRoot && b.IsRoot)
        {
            return 0;
        }

        var cmp = CompareFromRoot(a.Parent, b.Parent);
        return cmp != 0 ? cmp : CompareCurrentSegment(a, b);
    }

    private static int CompareCurrentSegment(Path x, Path y)
        => x switch
        {
            IndexerPathSegment ix when y is IndexerPathSegment iy
                => ix.Index.CompareTo(iy.Index),
            IndexerPathSegment when y is NamePathSegment
                => -1,
            NamePathSegment when y is IndexerPathSegment
                => 1,
            NamePathSegment nx when y is NamePathSegment ny
                => string.CompareOrdinal(nx.Name, ny.Name),
            _ => throw new NotSupportedException("Unexpected Path segment type.")
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

    /// <summary>
    /// Creates a new path from a list of elements.
    /// </summary>
    /// <param name="path">
    /// The path elements.
    /// </param>
    /// <returns>
    /// Returns a new path.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The path contains an element that is not supported.
    /// </exception>
    public static Path FromList(IReadOnlyList<object> path)
    {
        ArgumentNullException.ThrowIfNull(path);

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
                _ => throw new NotSupportedException()
            };
        }

        return segment;
    }

    public static Path Root => RootPathSegment.Instance;

    public static Path Parse(string s)
    {
        ArgumentException.ThrowIfNullOrEmpty(s);
        return ParseInternal(s);
    }

    private static Path ParseInternal(ReadOnlySpan<char> s)
    {
        if (s.IsEmpty || s is "/")
        {
            return Root;
        }

        var current = Root;
        var i = 0;

        while (i < s.Length)
        {
            if (s[i] == '/')
            {
                i++; // skip '/'

                var start = i;
                while (i < s.Length && s[i] != '/' && s[i] != '[')
                {
                    i++;
                }

                if (start == i)
                {
                    throw new FormatException(
                        $"Invalid path: empty name segment at position {start}.");
                }

                var nameSpan = s[start..i];
                current = current.Append(nameSpan.ToString()); // allocate string only once!
            }
            else if (s[i] == '[')
            {
                i++; // skip '['

                var start = i;
                while (i < s.Length && s[i] != ']')
                {
                    i++;
                }

                if (i == s.Length)
                {
                    throw new FormatException(
                        $"Invalid path: unterminated indexer at position {start}.");
                }

                var numberSpan = s[start..i];
                if (!int.TryParse(numberSpan, out var index) || index < 0)
                {
                    throw new FormatException(
                        $"Invalid path: invalid index '{numberSpan.ToString()}' at position {start}.");
                }

                current = current.Append(index);
                i++; // skip ']'
            }
            else
            {
                throw new FormatException(
                    $"Invalid path: unexpected character '{s[i]}' at position {i}.");
            }
        }

        return current;
    }

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
