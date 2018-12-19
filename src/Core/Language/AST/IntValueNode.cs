using System;
using System.Globalization;

namespace HotChocolate.Language
{
    public sealed class IntValueNode
        : IValueNode<string>
        , IEquatable<IntValueNode>
    {
        public IntValueNode(int value)
            : this(null, value.ToString("D", CultureInfo.InvariantCulture))
        {
        }

        public IntValueNode(string value)
            : this(null, value)
        {
        }

        public IntValueNode(
            Location location,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of an int value node cannot be null or empty.",
                    nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.IntValue;

        public Location Location { get; }

        public string Value { get; }

        /// <summary>
        /// Determines whether the specified <see cref="IntValueNode"/>
        /// is equal to the current <see cref="IntValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IntValueNode"/> to compare with the current
        /// <see cref="IntValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IntValueNode"/> is equal
        /// to the current <see cref="IntValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(IntValueNode other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Value.Equals(Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="IntValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="IntValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="IntValueNode"/>;
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
        /// the current <see cref="IntValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="IntValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="IntValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as IntValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="IntValueNode"/>
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
                return (Kind.GetHashCode() * 397)
                 ^ (Value.GetHashCode() * 97);
            }
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current
        /// <see cref="IntValueNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="IntValueNode"/>.
        /// </returns>
        public override string ToString()
        {
            return Value;
        }

        public IntValueNode WithLocation(Location location)
        {
            return new IntValueNode(location, Value);
        }

        public IntValueNode WithValue(string value)
        {
            return new IntValueNode(Location, value);
        }
    }
}
