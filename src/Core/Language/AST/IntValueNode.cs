using System.Buffers.Text;
using System;
using System.Globalization;

namespace HotChocolate.Language
{
    public sealed class IntValueNode
        : IValueNode<string>
        , IHasSpan
        , IEquatable<IntValueNode>
    {
        private string? _value;
        private ReadOnlyMemory<byte> _memory;
        private byte? _byteValue;
        private short? _shortValue;
        private int? _intValue;
        private long? _longValue;

        public IntValueNode(byte value)
            : this(null, value.ToString("D", CultureInfo.InvariantCulture))
        {
            _byteValue = value;
        }

        public IntValueNode(short value)
            : this(null, value.ToString("D", CultureInfo.InvariantCulture))
        {
            _shortValue = value;
        }

        public IntValueNode(int value)
            : this(null, value.ToString("D", CultureInfo.InvariantCulture))
        {
            _intValue = value;
        }

        public IntValueNode(long value)
            : this(null, value.ToString("D", CultureInfo.InvariantCulture))
        {
            _longValue = value;
        }

        public IntValueNode(string value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of an int value node mustn't be null or empty.",
                    nameof(value));
            }

            Location = location;
            _value = value;
        }

        public IntValueNode(Location? location, ReadOnlyMemory<byte> value)
        {
            if (value.IsEmpty)
            {
                throw new ArgumentNullException(
                    "The value of an int value node mustn't be empty.",
                    nameof(value));
            }

            Location = location;
            _memory = value;
        }

        public NodeKind Kind { get; } = NodeKind.IntValue;

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

        object? IValueNode.Value => Value;

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
        public bool Equals(IntValueNode? other)
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

            if (other is IntValueNode n)
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
        public override string? ToString()
        {
            return Value;
        }

        public byte ToByte()
        {
            if (_byteValue.HasValue)
            {
                return _byteValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out byte value, out _))
            {
                _byteValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public short ToInt16()
        {
            if (_shortValue.HasValue)
            {
                return _shortValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out short value, out _))
            {
                _shortValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public int ToInt32()
        {
            if (_intValue.HasValue)
            {
                return _intValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out int value, out _))
            {
                _intValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public long ToInt64()
        {
            if (_longValue.HasValue)
            {
                return _longValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out long value, out _))
            {
                _longValue = value;
                return value;
            }

            throw new InvalidFormatException();
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

        public IntValueNode WithLocation(Location? location)
        {
            return new IntValueNode(location, Value);
        }

        public IntValueNode WithValue(string value)
        {
            return new IntValueNode(Location, value);
        }
    }
}
