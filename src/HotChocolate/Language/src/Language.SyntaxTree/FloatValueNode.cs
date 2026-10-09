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
    private readonly ReadOnlyMemorySegment _memorySegment;
    private readonly Number _number;
    private readonly NumberKind _kind;
    private byte[]? _formatted;

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
        _number = new Number(value);
        _kind = NumberKind.Double;
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
        _number = new Number(value);
        _kind = NumberKind.Decimal;
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

    private FloatValueNode(Location? location, FloatValueNode original)
    {
        Location = location;
        Format = original.Format;
        _memorySegment = original._memorySegment;
        _number = original._number;
        _kind = original._kind;
        _formatted = original._formatted;
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
            return Encoding.UTF8.GetString(AsSpan());
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
        switch (_kind)
        {
            case NumberKind.Double:
                return (float)_number.Double;

            case NumberKind.Decimal:
                return (float)_number.Decimal;
        }

        if (!Utf8Parser.TryParse(_memorySegment.Span, out float value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "float");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="double"/>.
    /// </summary>
    public double ToDouble()
    {
        switch (_kind)
        {
            case NumberKind.Double:
                return _number.Double;

            case NumberKind.Decimal:
                return (double)_number.Decimal;
        }

        if (!Utf8Parser.TryParse(_memorySegment.Span, out double value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "double");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed float value as <see cref="decimal"/>.
    /// </summary>
    public decimal ToDecimal()
    {
        switch (_kind)
        {
            case NumberKind.Double:
                return (decimal)_number.Double;

            case NumberKind.Decimal:
                return _number.Decimal;
        }

        if (!Utf8Parser.TryParse(_memorySegment.Span, out decimal value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "decimal");
        }

        return value;
    }

    /// <summary>
    /// Gets a readonly span to access the float value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => AsMemorySegment().Span;

    public ReadOnlyMemorySegment AsMemorySegment()
    {
        if (_kind == NumberKind.Parsed)
        {
            return _memorySegment;
        }

        var formatted = _formatted;

        if (formatted is null)
        {
            formatted = FormatValue(_number, _kind);
            _formatted = formatted;
        }

        return new ReadOnlyMemorySegment(formatted);
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
        => new(location, this);

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

    private static byte[] FormatValue(Number number, NumberKind kind)
    {
        Span<byte> buffer = stackalloc byte[32];
        int written;

        if (kind == NumberKind.Double)
        {
            Utf8Formatter.TryFormat(number.Double, buffer, out written);
        }
        else
        {
            Utf8Formatter.TryFormat(number.Decimal, buffer, out written);
        }

#if NET8_0_OR_GREATER
        return buffer[..written].ToArray();
#else
        return buffer.Slice(0, written).ToArray();
#endif
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct Number
    {
        [FieldOffset(0)]
        public readonly double Double;

        [FieldOffset(0)]
        public readonly decimal Decimal;

        public Number(double value)
        {
            Decimal = default;
            Double = value;
        }

        public Number(decimal value)
        {
            Double = default;
            Decimal = value;
        }
    }

    private enum NumberKind : byte
    {
        Parsed,
        Double,
        Decimal
    }
}
