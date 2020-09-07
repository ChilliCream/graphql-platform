using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class FloatValueNode
        : IValueNode<string>
        , IEquatable<FloatValueNode>
        , IFloatValueLiteral
    {
        private ReadOnlyMemory<byte> _memory;
        private string? _stringValue;
        private float? _floatValue;
        private double? _doubleValue;
        private decimal? _decimalValue;

        public FloatValueNode(double value)
            : this(null, value)
        {
        }

        public FloatValueNode(Location? location, double value)
        {
            Location = location;
            _doubleValue = value;
            Format = FloatFormat.FixedPoint;
        }

        public FloatValueNode(decimal value)
            : this(null, value)
        {
        }

        public FloatValueNode(Location? location, decimal value)
        {
            Location = location;
            _decimalValue = value;
            Format = FloatFormat.FixedPoint;
        }

        public FloatValueNode(ReadOnlyMemory<byte> value, FloatFormat format)
            : this(null, value, format)
        {
        }

        public FloatValueNode(Location? location, ReadOnlyMemory<byte> value, FloatFormat format)
        {
            if (value.IsEmpty)
            {
                throw new ArgumentNullException(
                    "The value of a float value mustn't be empty.",
                    nameof(value));
            }

            Location = location;
            _memory = value;
            Format = format;
        }

        private FloatValueNode(Location? location, FloatFormat format)
        {
            Location = location;
            Format = format;
        }

        public SyntaxKind Kind { get; } = SyntaxKind.FloatValue;

        public Location? Location { get; }

        public FloatFormat Format { get; }

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

        object IValueNode.Value => Value;

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

        /// <summary>
        /// Determines whether the specified <see cref="FloatValueNode"/>
        /// is equal to the current <see cref="FloatValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="FloatValueNode"/> to compare with the current
        /// <see cref="FloatValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="FloatValueNode"/> is equal
        /// to the current <see cref="FloatValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(FloatValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (other._floatValue.HasValue
                && _floatValue.HasValue
                && other._floatValue.Value.Equals(_floatValue.Value))
            {
                return true;
            }

            if (other._doubleValue.HasValue
                && _doubleValue.HasValue
                && other._doubleValue.Value.Equals(_doubleValue.Value))
            {
                return true;
            }

            if (other._decimalValue.HasValue
                && _decimalValue.HasValue
                && other._decimalValue.Value.Equals(_decimalValue.Value))
            {
                return true;
            }

            return other.AsSpan().SequenceEqual(AsSpan());
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="FloatValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="FloatValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="FloatValueNode"/>;
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

            if (other is FloatValueNode f)
            {
                return Equals(f);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="FloatValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="FloatValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="FloatValueNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as FloatValueNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="FloatValueNode"/>
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

        public float ToSingle()
        {
            if (_floatValue.HasValue)
            {
                return _floatValue.Value;
            }

            char format = Format == FloatFormat.FixedPoint ? 'g' : 'e';
            if (Utf8Parser.TryParse(AsSpan(), out float value, out _, format))
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

            char format = Format == FloatFormat.FixedPoint ? 'g' : 'e';
            if (Utf8Parser.TryParse(AsSpan(), out double value, out _, format))
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

            char format = Format == FloatFormat.FixedPoint ? 'g' : 'e';
            if (Utf8Parser.TryParse(AsSpan(), out decimal value, out _, format))
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

                if (_floatValue.HasValue)
                {
                    Utf8Formatter.TryFormat(_floatValue.Value, buffer, out written, 'g');
                }
                else if (_doubleValue.HasValue)
                {
                    Utf8Formatter.TryFormat(_doubleValue.Value, buffer, out written, 'g');
                }
                else
                {
                    Utf8Formatter.TryFormat(_decimalValue!.Value, buffer, out written, 'g');
                }

                var memory = new Memory<byte>(new byte[written]);
                buffer.Slice(0, written).CopyTo(memory.Span);
                _memory = memory;
            }

            return _memory.Span;
        }

        public FloatValueNode WithLocation(Location? location)
        {
            return new FloatValueNode(location, Format)
            {
                _memory = _memory,
                _floatValue = _floatValue,
                _doubleValue = _doubleValue,
                _decimalValue = _decimalValue,
                _stringValue = Value
            };
        }

        public FloatValueNode WithValue(double value)
        {
            return new FloatValueNode(Location, value);
        }

        public FloatValueNode WithValue(decimal value)
        {
            return new FloatValueNode(Location, value);
        }

        public FloatValueNode WithValue(ReadOnlyMemory<byte> value, FloatFormat format)
        {
            return new FloatValueNode(Location, value, format);
        }

        public FloatValueNode WithValue(ReadOnlySpan<byte> value, FloatFormat format)
        {
            return new FloatValueNode(Location, value.ToArray(), format);
        }
    }
}
