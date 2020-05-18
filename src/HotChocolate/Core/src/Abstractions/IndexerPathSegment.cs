using System;
using System.Collections.Generic;

#nullable  enable

namespace HotChocolate
{
    /// <summary>
    /// An <see cref="IndexerPathSegment" /> represents a pointer to 
    /// an list element in the result structure.
    /// </summary>
    public sealed class IndexerPathSegment : NewPath
    {
        internal IndexerPathSegment(IPathSegment parent, int index)
        {
            Parent = parent;
            Depth = parent.Depth + 1;
            Index = index;
        }

        /// <inheritdoc />
        public override IPathSegment Parent { get; }

        /// <inheritdoc />
        public override int Depth { get; }

        /// <summary>
        /// Gets the <see cref="Index"/> which represents the position an element in a 
        /// list of the result structure.
        /// </summary>
        public int Index { get; }

        /// <inheritdoc />
        public override string Print()
        {
            return $"{Parent.Print()}[{Index}]";
        }
    }

    public abstract class NewPath : IPathSegment
    {
        internal NewPath() { }

        /// <inheritdoc />
        public abstract IPathSegment? Parent { get; }

        /// <inheritdoc />
        public abstract int Depth { get; }

        /// <inheritdoc />
        public IPathSegment Append(int index)
        {
            if (index < 0)
            {
                // TODO : ThrowHelper
                throw new ArgumentException();
            }

            return new IndexerPathSegment(this, index);
        }

        /// <inheritdoc />
        public IPathSegment Append(NameString name)
        {
            name.EnsureNotEmpty(nameof(name));
            return new NamePathSegment(this, name);
        }

        /// <inheritdoc />
        public abstract string Print();

        /// <inheritdoc />
        public IReadOnlyList<object> ToList()
        {
            var stack = new List<object>();
            IPathSegment? current = this;

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
        public static IPathSegment New(NameString name)
        {
            return new NamePathSegment(null, name);
        }
    }
}
