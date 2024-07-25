using System.Buffers.Text;
using System.Text;
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
    private ReadOnlyMemory<byte> _memory;
    private string? _stringValue;
    private byte? _byteValue;
    private sbyte? _sbyteValue;
    private short? _shortValue;
    private int? _intValue;
    private long? _longValue;
    private float? _floatValue;
    private double? _doubleValue;
    private decimal? _decimalValue;
    private ushort? _uShortValue;
    private uint? _uIntValue;
    private ulong? _uLongValue;

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(byte value)
        : this(null, value) { }

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
        _byteValue = value;
        _shortValue = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(short value)
        : this(null, value) { }

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
        _shortValue = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(int value)
        : this(null, value) { }

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
        _intValue = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(long value)
        : this(null, value) { }

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
        _longValue = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(sbyte value)
        : this(null, value) { }

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
        _sbyteValue = value;

        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
        var memory = new Memory<byte>(new byte[written]);
        buffer.Slice(0, written).CopyTo(memory.Span);
        _memory = memory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ushort value)
        : this(null, value) { }

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
        _uShortValue = value;

        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
        var memory = new Memory<byte>(new byte[written]);
        buffer.Slice(0, written).CopyTo(memory.Span);
        _memory = memory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(uint value)
        : this(null, value) { }

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
        _uIntValue = value;

        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
        var memory = new Memory<byte>(new byte[written]);
        buffer.Slice(0, written).CopyTo(memory.Span);
        _memory = memory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ulong value)
        : this(null, value) { }

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
        _uLongValue = value;

        Span<byte> buffer = stackalloc byte[32];
        Utf8Formatter.TryFormat(value, buffer, out var written);
        var memory = new Memory<byte>(new byte[written]);
        buffer.Slice(0, written).CopyTo(memory.Span);
        _memory = memory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(ReadOnlyMemory<byte> value)
        : this(null, value) { }

    /// <summary>
    /// Initializes a new instance of <see cref="IntValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The value.
    /// </param>
    public IntValueNode(Location? location, ReadOnlyMemory<byte> value)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentNullException(
                nameof(value),
                Resources.IntValueNode_ValueCannotBeEmpty);
        }

        Location = location;
        _memory = value;
    }

    private IntValueNode(Location? location)
    {
        Location = location;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.IntValue;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// The raw parsed string representation of the parsed value node.
    /// </summary>
    public unsafe string Value
    {
        get
        {
            if (_stringValue is null)
            {
                var span = AsSpan();

                fixed (byte* b = span)
                {
                    _stringValue = Encoding.UTF8.GetString(b, span.Length);
                }
            }
            return _stringValue;
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

    /// <summary>
    /// Reads the parsed int value as <see cref="byte"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="short"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="int"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="long"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="sbyte"/>.
    /// </summary>
    public sbyte ToSByte()
    {
        if (_sbyteValue.HasValue)
        {
            return _sbyteValue.Value;
        }

        if (Utf8Parser.TryParse(AsSpan(), out sbyte value, out _))
        {
            _sbyteValue = value;
            return value;
        }

        throw new InvalidFormatException();
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="ushort"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="uint"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="ulong"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="float"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="double"/>.
    /// </summary>
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

    /// <summary>
    /// Reads the parsed int value as <see cref="decimal"/>.
    /// </summary>
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

    /// <summary>
    /// Gets a readonly span to access the int value memory.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan()
        => AsMemory().Span;

    internal ReadOnlyMemory<byte> AsMemory()
    {
        if (!_memory.IsEmpty)
        {
            return _memory;
        }

        Span<byte> buffer = stackalloc byte[32];
        int written;

        if (_shortValue.HasValue)
        {
            Utf8Formatter.TryFormat(_shortValue.Value, buffer, out written);
        }
        else if (_intValue.HasValue)
        {
            Utf8Formatter.TryFormat(_intValue.Value, buffer, out written);
        }
        else if (_sbyteValue.HasValue)
        {
            Utf8Formatter.TryFormat(_sbyteValue.Value, buffer, out written);
        }
        else
        {
            Utf8Formatter.TryFormat(_longValue!.Value, buffer, out written);
        }

        var memory = new Memory<byte>(new byte[written]);
        buffer.Slice(0, written).CopyTo(memory.Span);
        _memory = memory;

        return _memory;
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
    public IntValueNode WithLocation(Location? location)
        => new(location)
        {
            _stringValue = _stringValue,
            _shortValue = _shortValue,
            _intValue = _intValue,
            _longValue = _longValue,
            _sbyteValue = _sbyteValue,
            _memory = _memory,
        };

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
    public IntValueNode WithValue(ReadOnlyMemory<byte> value) => new(Location, value);
}
