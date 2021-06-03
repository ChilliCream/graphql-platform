using System.Buffers.Text;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

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
        private ushort? _uShortValue;
        private uint? _uIntValue;
        private ulong? _uLongValue;

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

        public IntValueNode(ushort value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, ushort value)
        {
            Location = location;
            _uShortValue = value;
        }

        public IntValueNode(uint value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, uint value)
        {
            Location = location;
            _uIntValue = value;
        }

        public IntValueNode(ulong value)
            : this(null, value)
        {
        }

        public IntValueNode(Location? location, ulong value)
        {
            Location = location;
            _uLongValue = value;
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

        public SyntaxKind Kind { get; } = SyntaxKind.IntValue;

        public Location? Location { get; }

        public unsafe string Value
        {
            get
            {
                if (_stringValue is null)
                {
                    ReadOnlySpan<byte> span = AsSpan();
                    fixed (byte* b = span)
                    {
                        _stringValue = Encoding.UTF8.GetString(b, span.Length);
                    }
                }
                return _stringValue;
            }
        }

        object? IValueNode.Value => Value;

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

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
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

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

        public ushort ToUInt16()
        {
            if (_uShortValue.HasValue)
            {
                return _uShortValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out ushort value, out _))
            {
                _uShortValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public uint ToUInt32()
        {
            if (_uIntValue.HasValue)
            {
                return _uIntValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out uint value, out _))
            {
                _uIntValue = value;
                return value;
            }

            throw new InvalidFormatException();
        }

        public ulong ToUInt64()
        {
            if (_uLongValue.HasValue)
            {
                return _uLongValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out ulong value, out _))
            {
                _uLongValue = value;
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

        public decimal ToDecimal()
        {
            if (_decimalValue.HasValue)
            {
                return _decimalValue.Value;
            }

            if (Utf8Parser.TryParse(AsSpan(), out decimal value, out _, 'f'))
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
                var written = 0;

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
