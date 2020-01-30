using System.IO;
using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ListValueNode
        : IValueNode<IReadOnlyList<IValueNode>>
        , IEquatable<ListValueNode>
    {
        private int? _hash;
        private string? _stringValue;

        public ListValueNode(IValueNode item)
            : this(null, item)
        {
        }

        public ListValueNode(Location? location, IValueNode item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var items = new List<IValueNode>(1);
            items.Add(item);

            Location = location;
            Items = items.AsReadOnly();
        }

        public ListValueNode(
            IReadOnlyList<IValueNode> items)
            : this(null, items)
        {
        }

        public ListValueNode(
            Location? location,
            IReadOnlyList<IValueNode> items)
        {
            Location = location;
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public NodeKind Kind { get; } = NodeKind.ListValue;

        public Location? Location { get; }

        public IReadOnlyList<IValueNode> Items { get; }

        IReadOnlyList<IValueNode> IValueNode<IReadOnlyList<IValueNode>>.Value =>
            Items;

        object IValueNode.Value => Items;

        /// <summary>
        /// Determines whether the specified <see cref="ListValueNode"/>
        /// is equal to the current <see cref="ListValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="ListValueNode"/> to compare with the current
        /// <see cref="ListValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="ListValueNode"/> is equal
        /// to the current <see cref="ListValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ListValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other.Items.Count == Items.Count)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (!other.Items[i].Equals(Items[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="ListValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="ListValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="ListValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other is ListValueNode l)
            {
                return Equals(l);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="ListValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="ListValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="ListValueNode"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as ListValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="ListValueNode"/>
        /// object.
        /// </summary>
        /// <returns>
        /// A hash code for this instance that is suitable for use in
        /// hashing algorithms and data structures such as a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                if (_hash == null)
                {
                    var hash = 0;
                    for (int i = 0; i < Items.Count; i++)
                    {
                        hash = hash ^ (Items[i].GetHashCode() * 397);
                    }
                    _hash = hash;
                }

                return _hash.Value;
            }
        }

        public override string? ToString()
        {
            if (_stringValue is null)
            {
                _stringValue = QuerySyntaxSerializer.Serialize(this, true);
            }
            return _stringValue;
        }

        public ListValueNode WithLocation(Location? location)
        {
            return new ListValueNode(location, Items);
        }

        public ListValueNode WithItems(IReadOnlyList<IValueNode> items)
        {
            return new ListValueNode(Location, items);
        }
    }
}
