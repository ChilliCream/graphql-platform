using System.Buffers.Text;
using System.Runtime.InteropServices;
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
    private ReadOnlyMemorySegment _memorySegment;
    private byte[]? _value;

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
        _value = new byte[9];
        _value[0] = IntValueKind.Byte;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.Short;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.Int;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.Long;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.SByte;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.UShort;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.UInt;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
        _value = new byte[9];
        _value[0] = IntValueKind.ULong;
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(_value.AsSpan(1), in value);
#else
        MemoryMarshal.Write(_value.AsSpan(1), ref value);
#endif
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
    /// Reads the parsed int value as <see cref="byte"/>.
    /// </summary>
    public byte ToByte()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out byte value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid byte.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.Byte;
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
            IntValueKind.Byte => MemoryMarshal.Read<byte>(_value.AsSpan(1)),
            IntValueKind.SByte => CastByte(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastByte(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastByte(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastByte(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastByte(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastByte(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastByte(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static byte CastByte<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToInt64(value);
            if (l is < byte.MinValue or > byte.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return (byte)l;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="short"/>.
    /// </summary>
    public short ToInt16()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out short value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid short.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.Short;
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
            IntValueKind.Byte => CastInt16(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastInt16(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => MemoryMarshal.Read<short>(_value.AsSpan(1)),
            IntValueKind.UShort => CastInt16(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastInt16(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastInt16(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastInt16(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastInt16(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static short CastInt16<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToInt32(value);
            if (l is < short.MinValue or > short.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return (short)l;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="int"/>.
    /// </summary>
    public int ToInt32()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out int value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid int.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.Int;
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
            IntValueKind.Byte => CastInt32(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastInt32(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastInt32(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastInt32(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => MemoryMarshal.Read<int>(_value.AsSpan(1)),
            IntValueKind.UInt => CastInt32(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastInt32(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastInt32(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static int CastInt32<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToInt64(value);
            if (l is < int.MinValue or > int.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return (int)l;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="long"/>.
    /// </summary>
    public long ToInt64()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out long value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid long.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.Long;
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
            IntValueKind.Byte => CastInt64(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastInt64(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastInt64(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastInt64(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastInt64(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastInt64(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => MemoryMarshal.Read<long>(_value.AsSpan(1)),
            IntValueKind.ULong => CastInt64(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static long CastInt64<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToDecimal(value);
            if (l < long.MinValue || l > long.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return Convert.ToInt64(value);
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="sbyte"/>.
    /// </summary>
    public sbyte ToSByte()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out sbyte value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid sbyte.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.SByte;
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
            IntValueKind.Byte => CastSByte(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => MemoryMarshal.Read<sbyte>(_value.AsSpan(1)),
            IntValueKind.Short => CastSByte(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastSByte(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastSByte(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastSByte(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastSByte(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastSByte(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static sbyte CastSByte<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToInt32(value);
            if (l is < sbyte.MinValue or > sbyte.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return (sbyte)l;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="ushort"/>.
    /// </summary>
    public ushort ToUInt16()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out ushort value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid ushort.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.UShort;
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
            IntValueKind.Byte => CastUInt16(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastUInt16(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastUInt16(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => MemoryMarshal.Read<ushort>(_value.AsSpan(1)),
            IntValueKind.Int => CastUInt16(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastUInt16(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastUInt16(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastUInt16(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static ushort CastUInt16<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToInt32(value);
            if (l is < ushort.MinValue or > ushort.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return (ushort)l;
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="uint"/>.
    /// </summary>
    public uint ToUInt32()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out uint value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid uint.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.UInt;
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
            IntValueKind.Byte => CastUInt32(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastUInt32(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastUInt32(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastUInt32(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastUInt32(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => MemoryMarshal.Read<uint>(_value.AsSpan(1)),
            IntValueKind.Long => CastUInt32(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => CastUInt32(MemoryMarshal.Read<ulong>(_value.AsSpan(1))),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static uint CastUInt32<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToDecimal(value);
            if (l is < uint.MinValue or > uint.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return Convert.ToUInt32(value);
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="ulong"/>.
    /// </summary>
    public ulong ToUInt64()
    {
        if (_value is null)
        {
            if (_memorySegment.IsEmpty)
            {
                throw new InvalidOperationException("No numeric value was stored.");
            }

            if (!Utf8Parser.TryParse(_memorySegment.Span, out ulong value, out _))
            {
                throw new InvalidFormatException(
                    $"The value `{Encoding.UTF8.GetString(_memorySegment.Span)}` is not a valid ulong.");
            }

            Span<byte> buffer = stackalloc byte[9];
            buffer[0] = IntValueKind.ULong;
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
            IntValueKind.Byte => CastUInt64(MemoryMarshal.Read<byte>(_value.AsSpan(1))),
            IntValueKind.SByte => CastUInt64(MemoryMarshal.Read<sbyte>(_value.AsSpan(1))),
            IntValueKind.Short => CastUInt64(MemoryMarshal.Read<short>(_value.AsSpan(1))),
            IntValueKind.UShort => CastUInt64(MemoryMarshal.Read<ushort>(_value.AsSpan(1))),
            IntValueKind.Int => CastUInt64(MemoryMarshal.Read<int>(_value.AsSpan(1))),
            IntValueKind.UInt => CastUInt64(MemoryMarshal.Read<uint>(_value.AsSpan(1))),
            IntValueKind.Long => CastUInt64(MemoryMarshal.Read<long>(_value.AsSpan(1))),
            IntValueKind.ULong => MemoryMarshal.Read<ulong>(_value.AsSpan(1)),
            _ => throw new InvalidOperationException("Unsupported numeric kind.")
        };

        static ulong CastUInt64<T>(T value) where T : struct, IConvertible
        {
            var l = Convert.ToDecimal(value);
            if (l is < ulong.MinValue or > ulong.MaxValue)
            {
                throw new InvalidFormatException();
            }

            return Convert.ToUInt64(value);
        }
    }

    /// <summary>
    /// Reads the parsed int value as <see cref="float"/>.
    /// </summary>
    public float ToSingle()
    {
        if (!_memorySegment.IsEmpty)
        {
            return ParseSingle(_memorySegment.Span);
        }

        if (_value is null)
        {
            throw new InvalidOperationException("No numeric value was stored.");
        }

        Span<byte> buffer = stackalloc byte[32];
        var written = FormatValue(_value, buffer);
#if NET8_0_OR_GREATER
        return ParseSingle(buffer[..written]);
#else
        return ParseSingle(buffer.Slice(0, written));
#endif

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
        if (!_memorySegment.IsEmpty)
        {
            return ParseDouble(_memorySegment.Span);
        }

        if (_value is null)
        {
            throw new InvalidOperationException("No numeric value was stored.");
        }

        Span<byte> buffer = stackalloc byte[32];
        var written = FormatValue(_value, buffer);
#if NET8_0_OR_GREATER
        return ParseDouble(buffer[..written]);
#else
        return ParseDouble(buffer.Slice(0, written));
#endif

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
        if (!_memorySegment.IsEmpty)
        {
            return ParseDecimal(_memorySegment.Span);
        }

        if (_value is null)
        {
            throw new InvalidOperationException("No numeric value was stored.");
        }

        Span<byte> buffer = stackalloc byte[32];
        var written = FormatValue(_value, buffer);
#if NET8_0_OR_GREATER
        return ParseDecimal(buffer[..written]);
#else
        return ParseDecimal(buffer.Slice(0, written));
#endif

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
    public IntValueNode WithLocation(Location? location)
        => new(location) { _memorySegment = _memorySegment, _value = _value };

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

    private static int FormatValue(ReadOnlySpan<byte> value, Span<byte> utf8Buffer)
    {
        int written;
        var kind = value[0];
#if NET8_0_OR_GREATER
        value = value[1..];
#else
        value = value.Slice(1);
#endif

        var success = kind switch
        {
            IntValueKind.Byte => Utf8Formatter.TryFormat(MemoryMarshal.Read<byte>(value), utf8Buffer, out written),
            IntValueKind.SByte => Utf8Formatter.TryFormat(MemoryMarshal.Read<sbyte>(value), utf8Buffer, out written),
            IntValueKind.Short => Utf8Formatter.TryFormat(MemoryMarshal.Read<short>(value), utf8Buffer, out written),
            IntValueKind.UShort => Utf8Formatter.TryFormat(MemoryMarshal.Read<ushort>(value), utf8Buffer, out written),
            IntValueKind.Int => Utf8Formatter.TryFormat(MemoryMarshal.Read<int>(value), utf8Buffer, out written),
            IntValueKind.UInt => Utf8Formatter.TryFormat(MemoryMarshal.Read<uint>(value), utf8Buffer, out written),
            IntValueKind.Long => Utf8Formatter.TryFormat(MemoryMarshal.Read<long>(value), utf8Buffer, out written),
            IntValueKind.ULong => Utf8Formatter.TryFormat(MemoryMarshal.Read<ulong>(value), utf8Buffer, out written),
            _ => throw new InvalidOperationException("Invalid numeric kind.")
        };

        if (!success)
        {
            throw new InvalidOperationException("Failed to format numeric value.");
        }

        return written;
    }

    private static class IntValueKind
    {
        public const byte Byte = 1;
        public const byte SByte = 2;
        public const byte Short = 3;
        public const byte UShort = 4;
        public const byte Int = 5;
        public const byte UInt = 6;
        public const byte Long = 7;
        public const byte ULong = 8;
    }
}
