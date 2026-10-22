using System.Buffers;
using System.Runtime.CompilerServices;
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
