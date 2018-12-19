using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ObjectValueNode
        : IValueNode<IReadOnlyCollection<ObjectFieldNode>>
        , IEquatable<ObjectValueNode>
    {
        private int? _hash;

        public ObjectValueNode(
            params ObjectFieldNode[] fields)
            : this(null, fields)
        {
        }

        public ObjectValueNode(
            IReadOnlyList<ObjectFieldNode> fields)
            : this(null, fields)
        {
        }

        public ObjectValueNode(
            Location location,
            IReadOnlyList<ObjectFieldNode> fields)
        {
            Location = location;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public NodeKind Kind { get; } = NodeKind.ObjectValue;

        public Location Location { get; }

        public IReadOnlyList<ObjectFieldNode> Fields { get; }

        IReadOnlyCollection<ObjectFieldNode>
            IValueNode<IReadOnlyCollection<ObjectFieldNode>>.Value => Fields;

        /// <summary>
        /// Determines whether the specified <see cref="ObjectValueNode"/>
        /// is equal to the current <see cref="ObjectValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="ObjectValueNode"/> to compare with the current
        /// <see cref="ObjectValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="ObjectValueNode"/> is equal
        /// to the current <see cref="ObjectValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ObjectValueNode other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other.Fields.Count == Fields.Count)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    if (!other.Fields[i].Equals(Fields[i]))
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
        /// to the current <see cref="ObjectValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="ObjectValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="ObjectValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IValueNode other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other is ObjectValueNode o)
            {
                return Equals(o);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="ObjectValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="ObjectValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="ObjectValueNode"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as ObjectValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="ObjectValueNode"/>
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
                    for (var i = 0; i < Fields.Count; i++)
                    {
                        hash = hash ^ (Fields[i].GetHashCode() * 397);
                    }
                    _hash = hash;
                }

                return _hash.Value;
            }
        }

        public ObjectValueNode WithLocation(Location location)
        {
            return new ObjectValueNode(location, Fields);
        }

        public ObjectValueNode WithFields(
            IReadOnlyList<ObjectFieldNode> fields)
        {
            return new ObjectValueNode(Location, fields);
        }
    }
}
