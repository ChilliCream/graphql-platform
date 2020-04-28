using System;
using System.Collections.Generic;

#nullable  enable

namespace HotChocolate
{
    /// <summary>
    /// Represents the execution path to a field of the currently executed operation.
    /// </summary>
    public sealed class Path  : IEquatable<Path>
    {
        private Path(Path? parent, NameString name)
        {
            Parent = parent;
            Name = name;
            Index = null;
            Depth = parent?.Depth + 1 ?? 0;
        }

        private Path(Path? parent, NameString name, int index)
        {
            Parent = parent;
            Name = name;
            Index = index;
            Depth = parent?.Depth + 1 ?? 0;
        }

        /// <summary>
        /// Gets the parent path segment.
        /// </summary>
        public Path Parent { get; }

        /// <summary>
        /// Gets the name of this path segment.
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// If this path segment represents an element in a list this <see cref="Index"/>
        /// represents the position of that element in the list; otherwise it will be <c>null</c>.
        /// </summary>
        public int? Index { get; }

        /// <summary>
        /// Gets the count of segments this path contains.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Appends an element.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>Returns a new path segment pointing to an element in a list.</returns>
        public Path Append(int index)
        {
            return new Path(Parent, Name, index);
        }

        /// <summary>
        /// Appends a new path segment.
        /// </summary>
        /// <param name="name">The name of the path segment.</param>
        /// <returns>Returns a new path segment.</returns>
        public Path Append(NameString name)
        {
            return new Path(this, name);
        }


        /// <summary>
        /// Indicates whether the current <see cref="Path"/> is equal to another
        /// <see cref="Path"/> of the same type.
        /// </summary>
        /// <param name="other">
        /// A <see cref="Path"/> to compare with this <see cref="Path"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the current <see cref="Path"/> is equal to the
        /// <paramref name="other">other path</paramref> parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Path other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return ((Parent == null && other.Parent == null) || other.Parent.Equals(Parent))
                && string.Equals(other.Name, Name, StringComparison.Ordinal)
                && other.Index.Equals(Index);
        }


        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
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

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current <see cref="Path"/>.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (Parent?.GetHashCode() ?? 0) * 3;
                hash ^= (Name.GetHashCode() * 7);
                hash ^= (Index.GetHashCode() * 11);
                return hash;
            }
        }

        /// <summary>Returns a string that represents the current <see cref="Path"/>.</summary>
        /// <returns>A string that represents the current <see cref="Path"/>.</returns>
        public override string ToString()
        {
            string path = Parent == null ? string.Empty : Parent.ToString();
            return Index.HasValue ? $"{path}/{Name}[{Index}]" : $"{path}/{Name}";
        }

        [Obsolete("Use ToList")]
        public IReadOnlyList<object> ToCollection() => ToList();

        /// <summary>
        /// Creates a new list representing the current <see cref="Path"/>.
        /// </summary>
        /// <returns>
        /// Returns a new list representing the current <see cref="Path"/>.
        /// </returns>
        public IReadOnlyList<object> ToList()
        {
            var stack = new List<object>();
            Path current = this;

            while (current != null)
            {
                if (current.Index.HasValue)
                {
                    stack.Insert(0, current.Index);
                }
                stack.Insert(0, current.Name);
                current = current.Parent;
            }

            return stack;
        }

        /// <summary>
        /// Creates a root segment.
        /// </summary>
        /// <param name="name">The name of the root segment.</param>
        /// <returns>
        /// Returns a new root segment.
        /// </returns>
        public static Path New(NameString name)
        {
            return new Path(null, name);
        }
    }
}
