using System.Buffers.Text;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Language.Properties;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// An IntValue is specified without a decimal point or exponent but may be negative (ex. -123).
/// It must not have any leading 0.
/// </para>
/// <para>
/// An IntValue must not be followed by a Digit. In other words, an IntValue token is always
/// the longest possible valid sequence. The source characters 12 cannot be interpreted as
/// two tokens since 1 is followed by the Digit 2. This also means the source 00 is invalid
/// since it can neither be interpreted as a single token nor two 0 tokens.
/// </para>
/// <para>
/// An IntValue must not be followed by a . or NameStart.
/// If either . or ExponentIndicator follows then the token must only be interpreted as a
/// possible FloatValue. No other NameStart character can follow. For example the sequences
/// 0x123 and 123L have no valid lexical representations.
/// </para>
/// </summary>
public sealed class IntValueNode : IValueNode<string>, IIntValueLiteral
{
    private readonly ReadOnlyMemorySegment _memorySegment;

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(byte value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, byte value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(short value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, short value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(int value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, int value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(long value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, long value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(sbyte value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, sbyte value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ushort value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, ushort value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(uint value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, uint value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ulong value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, ulong value)
    {
        Location = location;
        _memorySegment = FormatValue(value);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ReadOnlyMemorySegment value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, ReadOnlyMemorySegment value)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentNullException(
                nameof(value),
                Resources.IntValueNode_ValueCannotBeEmpty);
        }

        Location = location;
        _memorySegment = value;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.IntValue;

    /// <inheritdoc />
    public Location? Location { get; }

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
            return Encoding.UTF8.GetString(_memorySegment.Span);
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
    /// Reads the parsed int value as <see cref="byte"/>.
    /// </summary>
    public byte ToByte()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out byte value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "byte");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="short"/>.
    /// </summary>
    public short ToInt16()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out short value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "short");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="int"/>.
    /// </summary>
    public int ToInt32()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out int value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "int");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="long"/>.
    /// </summary>
    public long ToInt64()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out long value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "long");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="sbyte"/>.
    /// </summary>
    public sbyte ToSByte()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out sbyte value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "sbyte");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="ushort"/>.
    /// </summary>
    public ushort ToUInt16()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out ushort value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "ushort");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="uint"/>.
    /// </summary>
    public uint ToUInt32()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out uint value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "uint");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="ulong"/>.
    /// </summary>
    public ulong ToUInt64()
    {
        if (!Utf8Parser.TryParse(_memorySegment.Span, out ulong value, out _))
        {
            throw ThrowHelper.InvalidNumericValue(_memorySegment.Span, "ulong");
        }

        return value;
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="float"/>.
    /// </summary>
    public float ToSingle()
    {
        return ParseSingle(_memorySegment.Span);

        static float ParseSingle(ReadOnlySpan<byte> span)
        {
            if (!Utf8Parser.TryParse(span, out float value, out _, standardFormat: 'f'))
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            return value;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="double"/>.
    /// </summary>
    public double ToDouble()
    {
        return ParseDouble(_memorySegment.Span);

        static double ParseDouble(ReadOnlySpan<byte> span)
        {
            if (!Utf8Parser.TryParse(span, out double value, out _, standardFormat: 'f'))
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            return value;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="decimal"/>.
    /// </summary>
    public decimal ToDecimal()
    {
        return ParseDecimal(_memorySegment.Span);

        static decimal ParseDecimal(ReadOnlySpan<byte> span)
        {
            if (!Utf8Parser.TryParse(span, out decimal value, out _, standardFormat: 'f'))
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            return value;
        }
    }

    /// <summary>
    /// Gets a readonly span to access the int value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => AsMemorySegment().Span;

    public ReadOnlyMemorySegment AsMemorySegment() => _memorySegment;

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
    public IntValueNode WithLocation(Location? location)
        => new(location, _memorySegment);

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
    public IntValueNode WithValue(byte value) => new(Location, value);

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
    public IntValueNode WithValue(sbyte value) => new(Location, value);

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
    public IntValueNode WithValue(short value) => new(Location, value);

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
    public IntValueNode WithValue(int value) => new(Location, value);

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
    public IntValueNode WithValue(long value) => new(Location, value);

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
    public IntValueNode WithValue(ReadOnlyMemorySegment value) => new(Location, value);

    private static ReadOnlyMemorySegment FormatValue(long value)
    {
        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
#if NET8_0_OR_GREATER
        return new ReadOnlyMemorySegment(buffer[..written].ToArray());
#else
        return new ReadOnlyMemorySegment(buffer.Slice(0, written).ToArray());
#endif
    }

    private static ReadOnlyMemorySegment FormatValue(ulong value)
    {
        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
#if NET8_0_OR_GREATER
        return new ReadOnlyMemorySegment(buffer[..written].ToArray());
#else
        return new ReadOnlyMemorySegment(buffer.Slice(0, written).ToArray());
#endif
    }
}
