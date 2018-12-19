using System;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a enum value literal.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Enum-Value
    /// </summary>
    public sealed class EnumValueNode
        : IValueNode<string>
        , IEquatable<EnumValueNode>
    {
        public EnumValueNode(object value)
            : this(null, value.ToString().ToUpperInvariant())
        {
        }

        public EnumValueNode(string value)
            : this(null, value)
        {
        }

        public EnumValueNode(
            Location location,
            string value)
        {
            Location = location;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NodeKind Kind { get; } = NodeKind.EnumValue;

        public Location Location { get; }

        public string Value { get; }

        /// <summary>
        /// Determines whether the specified <see cref="EnumValueNode"/>
        /// is equal to the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="EnumValueNode"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="EnumValueNode"/> is equal
        /// to the current <see cref="EnumValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(EnumValueNode other)
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
        /// to the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="EnumValueNode"/>;
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

            if (other is EnumValueNode e)
            {
                return Equals(e);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="EnumValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="EnumValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="EnumValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as EnumValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="EnumValueNode"/>
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
        /// <see cref="EnumValueNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="EnumValueNode"/>.
        /// </returns>
        public override string ToString()
        {
            return Value;
        }

        public EnumValueNode WithLocation(Location location)
        {
            return new EnumValueNode(location, Value);
        }

        public EnumValueNode WithValue(string value)
        {
            return new EnumValueNode(Location, value);
        }
    }
}
