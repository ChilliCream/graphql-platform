using System;
using System.Globalization;

namespace HotChocolate.Language
{
    public sealed class BooleanValueNode
        : IValueNode<bool>
        , IEquatable<BooleanValueNode?>
    {
        public BooleanValueNode(bool value)
            : this(null, value)
        {
        }

        public BooleanValueNode(
            Location? location,
            bool value)
        {
            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.BooleanValue;

        public Location? Location { get; }

        public bool Value { get; }

        object IValueNode.Value => Value;

        /// <summary>
        /// Determines whether the specified <see cref="BooleanValueNode"/>
        /// is equal to the current <see cref="BooleanValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="BooleanValueNode"/> to compare with the current
        /// <see cref="BooleanValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="BooleanValueNode"/> is equal
        /// to the current <see cref="BooleanValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(BooleanValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Value.Equals(Value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="BooleanValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="BooleanValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="BooleanValueNode"/>;
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

            if (other is BooleanValueNode b)
            {
                return Equals(b);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="BooleanValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="BooleanValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="BooleanValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as BooleanValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="BooleanValueNode"/>
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
        /// <see cref="BooleanValueNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="BooleanValueNode"/>.
        /// </returns>
        public override string? ToString()
        {
#if NETSTANDARD1_4
            return Value.ToString();
#else
            return Value.ToString(CultureInfo.InvariantCulture);
#endif
        }

        public BooleanValueNode WithLocation(Location? location) =>
            new BooleanValueNode(location, Value);

        public BooleanValueNode WithValue(bool value) =>
            new BooleanValueNode(Location, value);

        public static BooleanValueNode TrueLiteral { get; } = new BooleanValueNode(true);

        public static BooleanValueNode FalseLiteral { get; } = new BooleanValueNode(false);
    }
}
