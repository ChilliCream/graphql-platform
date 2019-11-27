using System;

namespace HotChocolate.Language
{
    public sealed class NullValueNode
        : IValueNode<object>
        , IEquatable<NullValueNode>
    {
        private NullValueNode()
        {
        }

        public NullValueNode(Location location)
        {
            Location = location;
        }

        public NodeKind Kind { get; } = NodeKind.NullValue;

        public Location Location { get; }

        public object Value { get; }

        /// <summary>
        /// Determines whether the specified <see cref="NullValueNode"/>
        /// is equal to the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="NullValueNode"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="NullValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NullValueNode other)
        {
            if (other is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>;
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

            if (other is NullValueNode n)
            {
                return Equals(n);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="NullValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as NullValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="NullValueNode"/>
        /// object.
        /// </summary>
        /// <returns>
        /// A hash code for this instance that is suitable for use in
        /// hashing algorithms and data structures such as a hash table.
        /// </returns>
        public override int GetHashCode() => 104729;

        public static NullValueNode Default { get; } = new NullValueNode();

        public NullValueNode WithLocation(Location location)
        {
            return new NullValueNode(location);
        }
    }
}
