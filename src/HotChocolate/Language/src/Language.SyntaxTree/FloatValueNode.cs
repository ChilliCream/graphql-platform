using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Language.Properties;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// A FloatValue includes either a decimal point (ex. 1.0) or an exponent (ex. 1e50) or
/// both (ex. 6.0221413e23) and may be negative. Like IntValue, it also must not have any
/// leading 0.
/// </para>
/// <para>
/// A FloatValue must not be followed by a Digit. In other words, a FloatValue token is always
/// the longest possible valid sequence. The source characters 1.23 cannot be interpreted as
/// two tokens since 1.2 is followed by the Digit 3.
/// </para>
/// <para>
/// A FloatValue must not be followed by a. For example, the sequence 1.23.4 cannot
/// be interpreted as two tokens (1.2, 3.4).
/// </para>
/// <para>
/// A FloatValue must not be followed by a NameStart. For example the sequence 0x1.2p3
/// has no valid lexical representation.
/// </para>
/// </summary>
public sealed class FloatValueNode : IValueNode<string>, IFloatValueLiteral
{
    private ReadOnlyMemorySegment _memorySegment;
    private byte[]? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(double value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(Location? location, double value)
    {
        Location = location;
        Format = FloatFormat.FixedPoint;
        _value = new byte[17];
        _value[0] = FloatValueKind.Double;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(decimal value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public FloatValueNode(Location? location, decimal value)
    {
        Location = location;
        Format = FloatFormat.FixedPoint;
        _value = new byte[17];
        _value[0] = FloatValueKind.Decimal;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <param name="format">
    /// The format of the parsed float value.
    /// </param>
    public FloatValueNode(ReadOnlyMemorySegment value, FloatFormat format)
        : this(null, value, format)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FloatValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    /// <param name="format">
    /// The format of the parsed float value.
    /// </param>
    public FloatValueNode(Location? location, ReadOnlyMemorySegment value, FloatFormat format)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentNullException(
                nameof(value),
                Resources.FloatValueNode_ValueEmpty);
        }

        Location = location;
        _memorySegment = value;
        Format = format;
    }

    private FloatValueNode(Location? location, FloatFormat format)
    {
        Location = location;
        Format = format;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.FloatValue;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the format of the parsed float value.
    /// </summary>
    public FloatFormat Format { get; }

    /// <summary>
    /// The raw parsed string representation of the parsed value node.
    /// </summary>
#if NET8_0_OR_GREATER
    public string Value
#else
    public unsafe string Value
#endif
    {
        get
        {
            if (!_memorySegment.IsEmpty)
            {
                return Encoding.UTF8.GetString(_memorySegment.Span);
            }

            if (_value is null)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            Span<byte> buffer = stackalloc byte[32];
            var written = FormatValue(_value, buffer);
#if NET8_0_OR_GREATER
            var value = buffer[..written];
#else
            var value = buffer.Slice(0, written);
#endif
            _memorySegment = new ReadOnlyMemorySegment(value.ToArray());
            return Encoding.UTF8.GetString(value);
        }
    }

    object IValueNode.Value => Value;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => [];

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => ToString(indented: true);

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
    public string ToString(bool indented) => this.Print(indented);

    /// <summary>
    /// Reads the parsed float value as <see cref="float"/>.
    /// </summary>
    public float ToSingle()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out float value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid float.");
            }

            Span<byte> buffer = stackalloc byte[17];
            buffer[0] = FloatValueKind.Single;
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(buffer[1..], in value);
#else
            MemoryMarshal.Write(buffer.Slice(1), ref value);
#endif
            _value = buffer.ToArray();
            return value;
        }

        return _value[0] switch
        {
            FloatValueKind.Single => MemoryMarshal.Read<float>(_value.AsSpan(1)),
            FloatValueKind.Double => (float)MemoryMarshal.Read<double>(_value.AsSpan(1)),
            FloatValueKind.Decimal => (float)MemoryMarshal.Read<decimal>(_value.AsSpan(1)),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="double"/>.
    /// </summary>
    public double ToDouble()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out double value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid double.");
            }

            Span<byte> buffer = stackalloc byte[17];
            buffer[0] = FloatValueKind.Double;
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(buffer[1..], in value);
#else
            MemoryMarshal.Write(buffer.Slice(1), ref value);
#endif
            _value = buffer.ToArray();
            return value;
        }

        return _value[0] switch
        {
            FloatValueKind.Single => MemoryMarshal.Read<float>(_value.AsSpan(1)),
            FloatValueKind.Double => MemoryMarshal.Read<double>(_value.AsSpan(1)),
            FloatValueKind.Decimal => (double)MemoryMarshal.Read<decimal>(_value.AsSpan(1)),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="decimal"/>.
    /// </summary>
    public decimal ToDecimal()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out decimal value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid decimal.");
            }

            Span<byte> buffer = stackalloc byte[17];
            buffer[0] = FloatValueKind.Decimal;
#if NET8_0_OR_GREATER
            MemoryMarshal.Write(buffer[1..], in value);
#else
            MemoryMarshal.Write(buffer.Slice(1), ref value);
#endif
            _value = buffer.ToArray();
            return value;
        }

        return _value[0] switch
        {
            FloatValueKind.Single => (decimal)MemoryMarshal.Read<float>(_value.AsSpan(1)),
            FloatValueKind.Double => (decimal)MemoryMarshal.Read<double>(_value.AsSpan(1)),
            FloatValueKind.Decimal => MemoryMarshal.Read<decimal>(_value.AsSpan(1)),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };
    }

    /// <summary>
    /// Gets a readonly span to access the float value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => AsMemorySegment().Span;

    public ReadOnlyMemorySegment AsMemorySegment()
    {
        if (!_memorySegment.IsEmpty)
        {
            return _memorySegment;
        }

        Span<byte> buffer = stackalloc byte[32];
        var written = FormatValue(_value, buffer);
#if NET8_0_OR_GREATER
        _memorySegment = new ReadOnlyMemorySegment(buffer[..written].ToArray());
#else
        _memorySegment = new ReadOnlyMemorySegment(buffer.Slice(0, written).ToArray());
#endif
        return _memorySegment;
    }

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public FloatValueNode WithLocation(Location? location)
        => new(location, Format) { _memorySegment = _memorySegment, _value = _value };

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(double value)
        => new(Location, value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(decimal value)
        => new(Location, value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <param name="format">
    /// The parsed float format.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public FloatValueNode WithValue(ReadOnlyMemorySegment value, FloatFormat format)
        => new(Location, value, format);

    private static int FormatValue(ReadOnlySpan<byte> value, Span<byte> utf8Buffer)
    {
        int w;
        var kind = value[0];
#if NET8_0_OR_GREATER
        value = value[1..];
#else
        value = value.Slice(1);
#endif

        var success = kind switch
        {
            FloatValueKind.Single => Utf8Formatter.TryFormat(MemoryMarshal.Read<float>(value), utf8Buffer, out w),
            FloatValueKind.Double => Utf8Formatter.TryFormat(MemoryMarshal.Read<double>(value), utf8Buffer, out w),
            FloatValueKind.Decimal => Utf8Formatter.TryFormat(MemoryMarshal.Read<decimal>(value), utf8Buffer, out w),
            _ => throw new InvalidOperationException("Invalid numeric kind.")
        };

        if (!success)
        {
            throw new InvalidOperationException("Failed to format numeric value.");
        }

        return w;
    }

    private static class FloatValueKind
    {
        public const byte Single = 1;
        public const byte Double = 2;
        public const byte Decimal = 3;
    }
}
