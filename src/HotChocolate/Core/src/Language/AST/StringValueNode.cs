using System;
using System.Text;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a string value literal.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-String-Value
    /// </summary>
    public sealed class StringValueNode
        : IValueNode<string>
        , IHasSpan
        , IEquatable<StringValueNode>
    {
        private ReadOnlyMemory<byte> _memory;
        private string? _value;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="StringValueNode"/> class.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public StringValueNode(string value)
            : this(null, value, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="StringValueNode"/> class.
        /// </summary>
        /// <param name="location">The source location.</param>
        /// <param name="value">The string value.</param>
        /// <param name="block">
        /// If set to <c>true</c> this instance represents a block string.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <c>null</c>.
        /// </exception>
        public StringValueNode(
            Location? location,
            string value,
            bool block)
        {
            Location = location;
            _value = value ?? throw new ArgumentNullException(nameof(value));
            Block = block;
        }

        public StringValueNode(
            Location? location,
            ReadOnlyMemory<byte> value,
            bool block)
        {
            Location = location;
            _memory = value;
            Block = block;
        }

        public NodeKind Kind { get; } = NodeKind.StringValue;

        public Location? Location { get; }

        public string Value
        {
            get
            {
                if (_value is null)
                {
                    _value = Utf8GraphQLReader.GetString(_memory.Span, Block);
                }
                return _value;
            }
        }

        object IValueNode.Value => Value;

        /// <summary>
        /// Gets a value indicating whether this <see cref="StringValueNode"/>
        /// was parsed from a block string.
        /// </summary>
        /// <value>
        /// <c>true</c> if this string value was parsed from a block string;
        /// otherwise, <c>false</c>.
        /// </value>
        public bool Block { get; }

        /// <summary>
        /// Determines whether the specified <see cref="StringValueNode"/>
        /// is equal to the current <see cref="StringValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="StringValueNode"/> to compare with the current
        /// <see cref="StringValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="StringValueNode"/> is equal
        /// to the current <see cref="StringValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(StringValueNode? other)
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
        /// to the current <see cref="StringValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="StringValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="StringValueNode"/>;
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

            if (other is StringValueNode s)
            {
                return Equals(s);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="StringValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="StringValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="StringValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as StringValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="StringValueNode"/>
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
        /// <see cref="StringValueNode"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="StringValueNode"/>.
        /// </returns>
        public override string? ToString()
        {
            return Value;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            if (_memory.IsEmpty)
            {
                _memory = Encoding.UTF8.GetBytes(_value!);
            }
            return _memory.Span;
        }

        public StringValueNode WithLocation(Location? location)
        {
            return new StringValueNode(location, Value, Block);
        }

        public StringValueNode WithValue(string value)
        {
            return new StringValueNode(Location, value, false);
        }

        public StringValueNode WithValue(string value, bool block)
        {
            return new StringValueNode(Location, value, block);
        }
    }
}
