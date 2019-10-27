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
        , IHasSpan
        , IEquatable<EnumValueNode?>
    {
        private ReadOnlyMemory<byte> _memory;
        private string? _value;

        public EnumValueNode(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            string? stringValue = value.ToString()?.ToUpperInvariant();

            if (stringValue is null)
            {
                throw new ArgumentException(
                    "The value string representation mustn't be null.",
                    nameof(value));
            }

            _value = stringValue;
        }

        public EnumValueNode(string value)
            : this(null, value)
        {
        }

        public EnumValueNode(Location? location, string value)
        {
            Location = location;
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public EnumValueNode(Location? location, ReadOnlyMemory<byte> value)
        {
            if (value.IsEmpty)
            {
                throw new ArgumentNullException(
                    "The value mustn't be empty.",
                    nameof(value));
            }

            Location = location;
            _memory = value;
        }

        public NodeKind Kind { get; } = NodeKind.EnumValue;

        public Location? Location { get; }

        public string Value
        {
            get
            {
                if (_value is null)
                {
                    _value = Utf8GraphQLReader.GetScalarValue(_memory.Span);
                }
                return _value;
            }
        }

        object IValueNode.Value => Value;

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
        public bool Equals(EnumValueNode? other)
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
        public override string? ToString()
        {
            return Value;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            if (_memory.IsEmpty)
            {
                int length = checked(_value!.Length * 4);
                Memory<byte> memory = new byte[length];
                Span<byte> span = memory.Span;
                int buffered = Utf8GraphQLParser.ConvertToBytes(_value, ref span);
                _memory = memory.Slice(0, buffered);
            }

            return _memory.Span;
        }

        public EnumValueNode WithLocation(Location? location)
        {
            return new EnumValueNode(location, Value);
        }

        public EnumValueNode WithValue(string value)
        {
            return new EnumValueNode(Location, value);
        }

        public EnumValueNode WithValue(Memory<byte> value)
        {
            return new EnumValueNode(Location, value);
        }
    }
}
