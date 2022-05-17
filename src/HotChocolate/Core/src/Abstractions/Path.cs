#nullable enable

using System;
using System.Collections.Generic;
using HotChocolate.Execution;

namespace HotChocolate;

/// <summary>
/// An <see cref="Path" /> represents a pointer to an element in the result structure.
/// </summary>
public abstract class Path : IEquatable<Path>
{
    /// <summary>
    /// Gets the parent path segment.
    /// </summary>
    public Path Parent { get; internal set; } = Root;

    /// <summary>
    /// Gets the count of segments this path contains.
    /// </summary>
    public int Depth { get; protected internal set; }

    /// <summary>
    /// Returns true if the Path is the root element
    /// </summary>
    public bool IsRoot => ReferenceEquals(this, Root);

    /// <summary>
    /// Generates a string that represents the current path.
    /// </summary>
    /// <returns>
    /// Returns a string that represents the current path.
    /// </returns>
    public abstract string Print();

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
        Path current = this;

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

    public abstract bool Equals(Path? other);

    /// <summary>
    /// Clones the path
    /// </summary>
    public abstract Path Clone();

    public sealed override bool Equals(object? obj)
        => obj switch
        {
            null => false,
            Path p => Equals(p),
            _ => false
        };

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="Path"/>.
    /// </returns>
    public abstract override int GetHashCode();

    internal static Path FromList(params object[] elements) =>
        FromList((IReadOnlyList<object>)elements);

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

        Path segment =
            PathFactory.Instance.New(path[0] is NameString s ? s : (string)path[0]);

        for (var i = 1; i < path.Count; i++)
        {
            segment = path[i] switch
            {
                NameString n => PathFactory.Instance.Append(segment, n),
                string n => PathFactory.Instance.Append(segment, n),
                int n => PathFactory.Instance.Append(segment, n),
                _ => throw new NotSupportedException("notsupported")
            };
        }

        return segment;
    }

    public static Path Root => RootPathSegment.Instance;

    private sealed class RootPathSegment : Path
    {
        private RootPathSegment()
        {
            Depth = -1;
        }

        /// <inheritdoc />
        public override string Print() => "/";

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
        public override Path Clone() => this;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Parent.GetHashCode() * 3;
                hash ^= Depth.GetHashCode() * 7;
                return hash;
            }
        }

        public static RootPathSegment Instance { get; } = new();
    }
}
