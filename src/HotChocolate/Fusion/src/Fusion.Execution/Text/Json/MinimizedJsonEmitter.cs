using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Writes minimized JSON tokens into a buffer writer without escaping. Property names are
/// written as pre-encoded tokens and values as raw JSON. The written bytes are committed to
/// the buffer writer by <see cref="Flush"/>.
/// </summary>
internal ref struct MinimizedJsonEmitter
{
    /// <summary>
    /// The minimum size hint of every span requested from the buffer writer.
    /// </summary>
    private const int MinimumSpanSize = 2 * 1024;

    /// <summary>
    /// The number of bytes copied at once for a short token whose buffer extends past it.
    /// </summary>
    private const int BlockSize = 32;

    private readonly IBufferWriter<byte> _output;
    private readonly int _maxDepth;
    private Span<byte> _span;
    private int _position;
    private int _depth;
    private bool _needsSeparator;

    /// <summary>
    /// Initializes a new emitter that starts at <paramref name="depth"/> and writes the
    /// first token without a list separator.
    /// </summary>
    public MinimizedJsonEmitter(IBufferWriter<byte> output, int depth, int maxDepth)
    {
        _output = output;
        _depth = depth;
        _maxDepth = maxDepth;
        _span = default;
        _position = 0;
        _needsSeparator = false;
    }

    public void WriteStartObject() => WriteStart(JsonConstants.OpenBrace);

    public void WriteStartArray() => WriteStart(JsonConstants.OpenBracket);

    public void WriteEndObject() => WriteEnd(JsonConstants.CloseBrace);

    public void WriteEndArray() => WriteEnd(JsonConstants.CloseBracket);

    /// <summary>
    /// Writes a property name token that already contains the quotes and the colon.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyNameToken)
    {
        var span = GetSpan(utf8PropertyNameToken.Length + 1);
        var written = 0;

        if (_needsSeparator)
        {
            span[written++] = JsonConstants.Comma;
        }

        utf8PropertyNameToken.CopyTo(span[written..]);
        _position += written + utf8PropertyNameToken.Length;
        _needsSeparator = false;
    }

    /// <summary>
    /// Writes the property name token held by the first <paramref name="length"/> bytes of
    /// <paramref name="buffer"/>; the token already contains the quotes and the colon.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WritePropertyName(ReadOnlySpan<byte> buffer, int length)
    {
        Debug.Assert((uint)length <= (uint)buffer.Length);

        if (length > BlockSize || buffer.Length < BlockSize)
        {
            WritePropertyName(buffer[..length]);
            return;
        }

        WriteBlock(ref MemoryMarshal.GetReference(buffer), length);
        _needsSeparator = false;
    }

    /// <summary>
    /// Writes a quoted, already escaped property name followed by a colon.
    /// </summary>
    public void WriteQuotedPropertyName(ReadOnlySpan<byte> utf8QuotedPropertyName)
    {
        var span = GetSpan(utf8QuotedPropertyName.Length + 2);
        var written = 0;

        if (_needsSeparator)
        {
            span[written++] = JsonConstants.Comma;
        }

        utf8QuotedPropertyName.CopyTo(span[written..]);
        written += utf8QuotedPropertyName.Length;
        span[written++] = JsonConstants.Colon;
        _position += written;
        _needsSeparator = false;
    }

    /// <summary>
    /// Writes a raw JSON value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRawValue(ReadOnlySpan<byte> utf8Value)
    {
        var span = GetSpan(utf8Value.Length + 1);
        var written = 0;

        if (_needsSeparator)
        {
            span[written++] = JsonConstants.Comma;
        }

        utf8Value.CopyTo(span[written..]);
        _position += written + utf8Value.Length;
        _needsSeparator = true;
    }

    /// <summary>
    /// Writes the raw JSON value held by the first <paramref name="length"/> bytes of
    /// <paramref name="buffer"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteRawValue(ReadOnlySpan<byte> buffer, int length)
    {
        Debug.Assert((uint)length <= (uint)buffer.Length);

        if (length > BlockSize || buffer.Length < BlockSize)
        {
            WriteRawValue(buffer[..length]);
            return;
        }

        WriteBlock(ref MemoryMarshal.GetReference(buffer), length);
        _needsSeparator = true;
    }

    /// <summary>
    /// Writes an already escaped string value. Quotes are added when the value
    /// does not start with one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
    {
        if (!utf8Value.IsEmpty && utf8Value[0] == JsonConstants.Quote)
        {
            WriteRawValue(utf8Value);
            return;
        }

        WriteUnquotedStringValue(utf8Value);
    }

    /// <summary>
    /// Writes the already escaped string value held by the first <paramref name="length"/>
    /// bytes of <paramref name="buffer"/>. Quotes are added when the value does not start
    /// with one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStringValue(ReadOnlySpan<byte> buffer, int length)
    {
        Debug.Assert((uint)length <= (uint)buffer.Length);

        if (length > 0 && buffer[0] == JsonConstants.Quote)
        {
            WriteRawValue(buffer, length);
            return;
        }

        WriteUnquotedStringValue(buffer[..length]);
    }

    private void WriteUnquotedStringValue(ReadOnlySpan<byte> utf8Value)
    {
        var span = GetSpan(utf8Value.Length + 3);
        var written = 0;

        if (_needsSeparator)
        {
            span[written++] = JsonConstants.Comma;
        }

        span[written++] = JsonConstants.Quote;
        utf8Value.CopyTo(span[written..]);
        written += utf8Value.Length;
        span[written++] = JsonConstants.Quote;
        _position += written;
        _needsSeparator = true;
    }

    /// <summary>
    /// Begins a raw value that is written in several parts.
    /// </summary>
    public void WriteRawValueStart()
    {
        if (_needsSeparator)
        {
            var span = GetSpan(1);
            span[0] = JsonConstants.Comma;
            _position++;
        }
    }

    /// <summary>
    /// Writes the next part of a raw value started with <see cref="WriteRawValueStart"/>.
    /// </summary>
    public void WriteRawValuePart(ReadOnlySpan<byte> utf8Json)
    {
        var span = GetSpan(utf8Json.Length);
        utf8Json.CopyTo(span);
        _position += utf8Json.Length;
    }

    /// <summary>
    /// Completes a raw value started with <see cref="WriteRawValueStart"/>.
    /// </summary>
    public void WriteRawValueEnd() => _needsSeparator = true;

    /// <summary>
    /// Commits the written bytes to the buffer writer.
    /// </summary>
    public void Flush()
    {
        if (_position > 0)
        {
            _output.Advance(_position);
        }

        _span = default;
        _position = 0;
    }

    private void WriteStart(byte token)
    {
        if (_depth >= _maxDepth)
        {
            ThrowHelper.ThrowInvalidOperationException_MaxDepthExceeded();
        }

        var span = GetSpan(2);
        var written = 0;

        if (_needsSeparator)
        {
            span[written++] = JsonConstants.Comma;
        }

        span[written++] = token;
        _position += written;
        _depth++;
        _needsSeparator = false;
    }

    private void WriteEnd(byte token)
    {
        GetSpan(1)[0] = token;
        _position++;
        _needsSeparator = true;

        if (_depth != 0)
        {
            _depth--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<byte> GetSpan(int size)
    {
        if (_span.Length - _position < size)
        {
            Grow(size);
        }

        return _span[_position..];
    }

    /// <summary>
    /// Writes the separator if one is needed, followed by the first <paramref name="length"/>
    /// bytes of the <see cref="BlockSize"/> readable bytes at <paramref name="source"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBlock(ref byte source, int length)
    {
        Debug.Assert((uint)length <= BlockSize);

        if (_span.Length - _position < BlockSize + 1)
        {
            Grow(BlockSize + 1);
        }

        ref var destination = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), _position);
        var separator = _needsSeparator ? 1 : 0;

        // The block overwrites the comma when no separator is needed, and the bytes it writes
        // past the token stay uncommitted until the next write overwrites them.
        destination = JsonConstants.Comma;
        destination = ref Unsafe.Add(ref destination, separator);
        Vector128.LoadUnsafe(ref source).StoreUnsafe(ref destination);
        Vector128.LoadUnsafe(ref source, 16).StoreUnsafe(ref destination, 16);
        _position += separator + length;
    }

    private void Grow(int size)
    {
        if (_position > 0)
        {
            _output.Advance(_position);
        }

        _span = _output.GetSpan(Math.Max(size, MinimumSpanSize));
        _position = 0;
    }
}
