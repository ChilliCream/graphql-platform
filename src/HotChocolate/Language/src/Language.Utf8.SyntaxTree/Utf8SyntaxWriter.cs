using System.Buffers;
using System.Text;

namespace HotChocolate.Language;

/// <summary>
/// Writes GraphQL source text to a buffer writer, either as plain UTF-8 text or as the content of
/// a JSON string value. In JSON mode the quotation mark, the reverse solidus and every control
/// character are replaced by their JSON escape sequence, while all other bytes, including
/// multi-byte UTF-8 sequences, are written unchanged. The delimiters that frame the JSON string
/// value are written by <see cref="WriteDelimiter"/>.
/// </summary>
internal readonly struct Utf8SyntaxWriter
{
#if NET8_0_OR_GREATER
    private static readonly SearchValues<byte> s_escapedBytes = CreateEscapedBytes();
    private static readonly SearchValues<char> s_escapedChars = CreateEscapedChars();
#endif
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    private readonly IBufferWriter<byte> _writer;
    private readonly bool _escape;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8SyntaxWriter"/> struct.
    /// </summary>
    /// <param name="writer">
    /// The buffer writer that receives the UTF-8 encoded output.
    /// </param>
    /// <param name="escape">
    /// <see langword="true"/> to write the output as the content of a JSON string value;
    /// otherwise, <see langword="false"/> to write it as plain GraphQL source text.
    /// </param>
    internal Utf8SyntaxWriter(IBufferWriter<byte> writer, bool escape)
    {
        _writer = writer;
        _escape = escape;
    }

    /// <summary>
    /// Writes the delimiter that opens or closes the JSON string value. Nothing is written when
    /// the output is plain GraphQL source text.
    /// </summary>
    internal void WriteDelimiter()
    {
        if (_escape)
        {
            _writer.Write("\""u8);
        }
    }

    /// <summary>
    /// Writes <paramref name="value"/> to the underlying buffer writer.
    /// </summary>
    /// <param name="value">
    /// The UTF-8 encoded GraphQL source text.
    /// </param>
    internal void Write(ReadOnlySpan<byte> value)
    {
        if (!_escape)
        {
            _writer.Write(value);
            return;
        }

        while (!value.IsEmpty)
        {
            var index = IndexOfEscaped(value);

            if (index < 0)
            {
                _writer.Write(value);
                return;
            }

            if (index > 0)
            {
                _writer.Write(value.Slice(0, index));
            }

            WriteEscapeSequence(value[index]);
            value = value.Slice(index + 1);
        }
    }

    /// <summary>
    /// Encodes <paramref name="value"/> as UTF-8 and writes it to the underlying buffer writer.
    /// </summary>
    /// <param name="value">
    /// The GraphQL source text.
    /// </param>
    internal void Write(string value)
    {
        if (!_escape || IndexOfEscaped(value.AsSpan()) < 0)
        {
            WriteUtf8(value);
            return;
        }

        var start = 0;

        while (start < value.Length)
        {
            var index = IndexOfEscaped(value.AsSpan(start));

            if (index < 0)
            {
                WriteUtf8(value, start, value.Length - start);
                return;
            }

            if (index > 0)
            {
                WriteUtf8(value, start, index);
            }

            WriteEscapeSequence((byte)value[start + index]);
            start += index + 1;
        }
    }

    private void WriteUtf8(string value)
    {
        var byteCount = s_utf8.GetByteCount(value);
        var destination = _writer.GetSpan(byteCount);
        _writer.Advance(s_utf8.GetBytes(value, destination));
    }

    private void WriteUtf8(string value, int start, int length)
    {
#if NET8_0_OR_GREATER
        var segment = value.AsSpan(start, length);
        var byteCount = s_utf8.GetByteCount(segment);
        var destination = _writer.GetSpan(byteCount);
        _writer.Advance(s_utf8.GetBytes(segment, destination));
#else
        WriteUtf8(value.Substring(start, length));
#endif
    }

    private void WriteEscapeSequence(byte value)
    {
        switch (value)
        {
            case (byte)'"':
                _writer.Write("\\\""u8);
                return;

            case (byte)'\\':
                _writer.Write("\\\\"u8);
                return;

            case (byte)'\n':
                _writer.Write("\\n"u8);
                return;

            case (byte)'\r':
                _writer.Write("\\r"u8);
                return;

            case (byte)'\t':
                _writer.Write("\\t"u8);
                return;

            case (byte)'\b':
                _writer.Write("\\b"u8);
                return;

            case (byte)'\f':
                _writer.Write("\\f"u8);
                return;

            default:
                var destination = _writer.GetSpan(6);
                destination[0] = (byte)'\\';
                destination[1] = (byte)'u';
                destination[2] = (byte)'0';
                destination[3] = (byte)'0';
                destination[4] = HexDigits[value >> 4];
                destination[5] = HexDigits[value & 0xF];
                _writer.Advance(6);
                return;
        }
    }

    private static ReadOnlySpan<byte> HexDigits => "0123456789ABCDEF"u8;

#if NET8_0_OR_GREATER
    private static int IndexOfEscaped(ReadOnlySpan<byte> value)
        => value.IndexOfAny(s_escapedBytes);

    private static int IndexOfEscaped(ReadOnlySpan<char> value)
        => value.IndexOfAny(s_escapedChars);

    private static SearchValues<byte> CreateEscapedBytes()
    {
        Span<byte> values = stackalloc byte[34];

        for (var i = 0; i < 32; i++)
        {
            values[i] = (byte)i;
        }

        values[32] = (byte)'"';
        values[33] = (byte)'\\';
        return SearchValues.Create(values);
    }

    private static SearchValues<char> CreateEscapedChars()
    {
        Span<char> values = stackalloc char[34];

        for (var i = 0; i < 32; i++)
        {
            values[i] = (char)i;
        }

        values[32] = '"';
        values[33] = '\\';
        return SearchValues.Create(values);
    }
#else
    private static int IndexOfEscaped(ReadOnlySpan<byte> value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (current < 0x20 || current is (byte)'"' or (byte)'\\')
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfEscaped(ReadOnlySpan<char> value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (current < 0x20 || current is '"' or '\\')
            {
                return i;
            }
        }

        return -1;
    }
#endif
}
