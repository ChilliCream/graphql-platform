using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Threading;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate;

public abstract partial class Path : IEquatable<Path>
{
    private static readonly Dictionary<string, Path> _cache = new();
    private static readonly ReaderWriterLockSlim _sync = new();
    private readonly string _pathString;

    internal Path(string pathString)
    {
        _pathString = pathString ?? throw new ArgumentNullException(nameof(pathString));
    }

    /// <summary>
    /// Gets the parent path segment.
    /// </summary>
    public abstract Path? Parent { get; }

    /// <summary>
    /// Gets the count of segments this path contains.
    /// </summary>
    public abstract int Depth { get; }

    /// <summary>
    /// Appends an element.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <returns>Returns a new path segment pointing to an element in a list.</returns>
    public virtual IndexerPathSegment Append(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var pathString = CreatePath(_pathString, index);

        _sync.EnterUpgradeableReadLock();

        try
        {
            if (_cache.TryGetValue(pathString, out Path? cachedPath))
            {
                return (IndexerPathSegment)cachedPath;
            }

            _sync.EnterWriteLock();

            try
            {
                var newPath = new IndexerPathSegment(this, index, pathString);
#if NETCOREAPP3_1_OR_GREATER
                _cache.TryAdd(pathString, newPath);
#else
                _cache[pathString] = newPath;
#endif
                return newPath;
            }
            finally
            {
                _sync.ExitWriteLock();
            }
        }
        finally
        {
            _sync.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Appends a new path segment.
    /// </summary>
    /// <param name="name">The name of the path segment.</param>
    /// <returns>Returns a new path segment.</returns>
    public virtual NamePathSegment Append(NameString name)
        => CreateNamePathSegment(_pathString, name, this);

    /// <summary>
    /// Generates a string that represents the current path.
    /// </summary>
    /// <returns>
    /// Returns a string that represents the current path.
    /// </returns>
    public string Print() => _pathString;

    /// <summary>
    /// Creates a new list representing the current <see cref="Path"/>.
    /// </summary>
    /// <returns>
    /// Returns a new list representing the current <see cref="Path"/>.
    /// </returns>
    public IReadOnlyList<object> ToList()
    {
        if (this is RootPathSegment)
        {
            return Array.Empty<object>();
        }

        var stack = new List<object>();
        Path? current = this;

        while (current != null)
        {
            switch (current)
            {
                case IndexerPathSegment indexer:
                    stack.Insert(0, indexer.Index);
                    break;

                case NamePathSegment name:
                    stack.Insert(0, name.Name);
                    break;

                case RootPathSegment:
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

    public sealed override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, null))
        {
            return false;
        }

        if (obj is Path p)
        {
            return Equals(p);
        }

        return false;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="Path"/>.
    /// </returns>
    public abstract override int GetHashCode();

    /// <summary>
    /// Creates a root segment.
    /// </summary>
    /// <param name="name">The name of the root segment.</param>
    /// <returns>
    /// Returns a new root segment.
    /// </returns>
    public static NamePathSegment New(NameString name)
        => CreateNamePathSegment(string.Empty, name, Root);

    public static RootPathSegment Root => RootPathSegment.Instance;

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

        Path segment = New((string)path[0]);

        for (var i = 1; i < path.Count; i++)
        {
            segment = path[i] switch
            {
                NameString n => segment.Append(n),
                string s => segment.Append(s),
                int n => segment.Append(n),
                _ => throw new NotSupportedException(
                    AbstractionResources.Path_WithPath_Path_Value_NotSupported)
            };
        }

        return segment;
    }
}
