using System;
using System.Collections.Generic;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate
{
    public abstract class Path : IEquatable<Path>
    {
        internal Path() { }

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
                // TODO : ThrowHelper
                throw new ArgumentException();
            }

            return new IndexerPathSegment(this, index);
        }

        /// <summary>
        /// Appends a new path segment.
        /// </summary>
        /// <param name="name">The name of the path segment.</param>
        /// <returns>Returns a new path segment.</returns>
        public virtual NamePathSegment Append(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));
            return new NamePathSegment(this, name);
        }

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
        {
            return new NamePathSegment(null, name);
        }

        public static RootPathSegment Root { get; } = RootPathSegment.Instance;

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
}
