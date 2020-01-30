using System.Buffers.Text;
using System;

namespace HotChocolate.Language
{
    public sealed class IntValueNode
        : IValueNode<string>
        , IEquatable<IntValueNode>
        , IIntValueLiteral
    {
        private ReadOnlyMemory<byte> _memory;
        private string? _stringValue;
        private byte? _byteValue;
        private short? _shortValue;
        private int? _intValue;
        private long? _longValue;
        private float? _floatValue;
        private double? _doubleValue;
        private decimal? _decimalValue;

        public IntValueNode(byte value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, byte value)
        {
            Location = location;
            _byteValue = value;
            _shortValue = value;
        }

        public IntValueNode(short value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, short value)
        {
            Location = location;
            _shortValue = value;
        }

        public IntValueNode(int value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, int value)
        {
            Location = location;
            _intValue = value;
        }

        public IntValueNode(long value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, long value)
        {
            Location = location;
            _longValue = value;
        }

        public IntValueNode(ReadOnlyMemory<byte> value)
            : this(null, value)
        {
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

        private IntValueNode(Location? location)
        {
            Location = location;
        }

        public NodeKind Kind { get; } = NodeKind.IntValue;

        public Location? Location { get; }

        public string Value
        {
            get
            {
                if (_stringValue is null)
                {
                    _stringValue = Utf8GraphQLReader.GetScalarValue(AsSpan());
                }
                return _stringValue;
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

        public float ToSingle()
        {
            if (_floatValue.HasValue)
            {
                return _floatValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out float value, out _, 'f'))
            {
                _floatValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public double ToDouble()
        {
            if (_doubleValue.HasValue)
            {
                return _doubleValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out double value, out _, 'f'))
            {
                _doubleValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public Decimal ToDecimal()
        {
            if (_decimalValue.HasValue)
            {
                return _decimalValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out Decimal value, out _, 'f'))
            {
                _decimalValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            if (_memory.IsEmpty)
            {
                Span<byte> buffer = stackalloc byte[32];
                int written = 0;

                if (_shortValue.HasValue)
                {
                    Utf8Formatter.TryFormat(_shortValue.Value, buffer, out written);
                }
                else if (_intValue.HasValue)
                {
                    Utf8Formatter.TryFormat(_intValue.Value, buffer, out written);
                }
                else
                {
                    Utf8Formatter.TryFormat(_longValue!.Value, buffer, out written);
                }

                var memory = new Memory<byte>(new byte[written]);
                buffer.Slice(0, written).CopyTo(memory.Span);
                _memory = memory;
            }

            return _memory.Span;
        }

        public IntValueNode WithLocation(Location? location)
        {
            return new IntValueNode(location)
            {
                _stringValue = _stringValue,
                _shortValue = _shortValue,
                _intValue = _intValue,
                _longValue = _longValue,
                _memory = _memory
            };
        }

        public IntValueNode WithValue(byte value)
        {
            return new IntValueNode(Location, value);
        }

        public IntValueNode WithValue(short value)
        {
            return new IntValueNode(Location, value);
        }

        public IntValueNode WithValue(int value)
        {
            return new IntValueNode(Location, value);
        }

        public IntValueNode WithValue(long value)
        {
            return new IntValueNode(Location, value);
        }

        public IntValueNode WithValue(ReadOnlyMemory<byte> value)
        {
            return new IntValueNode(Location, value);
        }
    }
}
