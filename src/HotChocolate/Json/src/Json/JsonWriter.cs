using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Unicode;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    private readonly JsonWriterOptions _options;
    private IBufferWriter<byte> _writer;

    // The highest order bit of _currentDepth is used to discern whether we are writing the first item in a list or not.
    // if (_currentDepth >> 31) == 1, add a list separator before writing the item
    // else, no list separator is needed since we are writing the first item.
    private int _currentDepth;
    private JsonTokenType _tokenType;

    // Cache indentation settings from JsonWriterOptions to avoid recomputing them in the hot path.
    private readonly byte _indentByte;
    private readonly int _indentLength;

    // A length of 1 will emit LF for indented writes, a length of 2 will emit CRLF. Other values are invalid.
    private readonly int _newLineLength;
    private readonly bool _indented;
    private readonly int _maxDepth;

    // Deferred property name support for transparent null field omission.
    // When IgnoreNullFields is true, WritePropertyName writes to _deferBuffer instead of
    // _writer. If the next value is null, we discard the deferred bytes (rollback).
    // If the next value is non-null, we flush the deferred bytes to the real writer first.
    private DeferBuffer? _deferBuffer;
    private IBufferWriter<byte>? _realWriter;
    private int _savedCurrentDepth;
    private JsonTokenType _savedTokenType;

    // Container type tracking for transparent null list element omission.
    // Bit N = 1 means depth N is an array, bit N = 0 means object.
    // Supports up to 64 levels of nesting.
    private long _containerTypeStack;

    public JsonWriter(
        IBufferWriter<byte> writer,
        JsonWriterOptions options,
        JsonNullIgnoreCondition nullIgnoreCondition = JsonNullIgnoreCondition.None)
    {
        _writer = writer;
        _options = options;
        NullIgnoreCondition = nullIgnoreCondition;

#if NET9_0_OR_GREATER
        Debug.Assert(options.NewLine is "\n" or "\r\n", "Invalid NewLine string.");
        _newLineLength = options.NewLine.Length;
        _indentByte = (byte)_options.IndentCharacter;
        _indentLength = options.IndentSize;
#else
        _newLineLength = 1;
        _indentByte = (byte)' ';
        _indentLength = 2;
#endif
        _indented = options.Indented;
        _maxDepth = options.MaxDepth == 0 ? 64 : options.MaxDepth;
    }

    /// <summary>
    /// Resets the writer so it can be reused with a new buffer,
    /// avoiding allocations when the writer is pooled.
    /// </summary>
    /// <param name="writer">The new buffer writer to write to.</param>
    public void Reset(IBufferWriter<byte> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
        _currentDepth = 0;
        _tokenType = default;
        _containerTypeStack = 0;
        _realWriter = null;
        _savedCurrentDepth = 0;
        _savedTokenType = default;
    }

    /// <summary>
    /// Gets the custom behavior when writing JSON using
    /// the <see cref="Utf8JsonWriter"/> which indicates whether to format the output
    /// while writing and whether to skip structural JSON validation or not.
    /// </summary>
    public JsonWriterOptions Options => _options;

    /// <summary>
    /// Gets the null ignore condition that controls whether null values
    /// are omitted from the JSON output.
    /// </summary>
    public JsonNullIgnoreCondition NullIgnoreCondition { get; set; }

    /// <summary>
    /// Gets a value indicating whether null fields should be omitted
    /// when writing JSON objects.
    /// </summary>
    public bool IgnoreNullFields
        => (NullIgnoreCondition & JsonNullIgnoreCondition.Fields) == JsonNullIgnoreCondition.Fields;

    /// <summary>
    /// Gets a value indicating whether null elements should be omitted
    /// when writing JSON arrays.
    /// </summary>
    public bool IgnoreNullListElements
        => (NullIgnoreCondition & JsonNullIgnoreCondition.Lists) == JsonNullIgnoreCondition.Lists;

    private int Indentation => CurrentDepth * _indentLength;

    internal JsonTokenType TokenType => _tokenType;

    /// <summary>
    /// Tracks the recursive depth of the nested objects / arrays within the JSON text
    /// written so far. This provides the depth of the current token.
    /// </summary>
    public int CurrentDepth => _currentDepth & JsonConstants.RemoveFlagsBitMask;

    private bool HasDeferredPropertyName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _realWriter is not null;
    }

    private bool IsInArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var depth = CurrentDepth;
            if (depth is 0 or > 64)
            {
                return false;
            }

            return (_containerTypeStack & (1L << (depth - 1))) != 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushDeferredPropertyName()
    {
        if (_realWriter is not null)
        {
            FlushDeferredPropertyNameSlow();
        }
    }

    private void FlushDeferredPropertyNameSlow()
    {
        var deferred = _deferBuffer!;
        _writer = _realWriter!;
        _realWriter = null;

        var span = _writer.GetSpan(deferred.WrittenCount);
        deferred.WrittenSpan.CopyTo(span);
        _writer.Advance(deferred.WrittenCount);
    }

    private void DiscardDeferredPropertyName()
    {
        _writer = _realWriter!;
        _realWriter = null;
        _currentDepth = _savedCurrentDepth;
        _tokenType = _savedTokenType;
    }

    private void BeginDeferPropertyName()
    {
        _savedCurrentDepth = _currentDepth;
        _savedTokenType = _tokenType;

        _deferBuffer ??= new DeferBuffer();
        _deferBuffer.Reset();

        _realWriter = _writer;
        _writer = _deferBuffer;
    }

    private void SetContainerTypeArray()
    {
        var depth = CurrentDepth;

        if (depth is > 0 and <= 64)
        {
            _containerTypeStack |= 1L << (depth - 1);
        }
    }

    private void SetContainerTypeObject()
    {
        var depth = CurrentDepth;

        if (depth is > 0 and <= 64)
        {
            _containerTypeStack &= ~(1L << (depth - 1));
        }
    }

    /// <summary>
    /// Writes the beginning of a JSON array.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000
    /// OR if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteStartArray()
    {
        FlushDeferredPropertyName();
        WriteStart(JsonConstants.OpenBracket);
        _tokenType = JsonTokenType.StartArray;
        SetContainerTypeArray();
    }

    /// <summary>
    /// Writes the beginning of a JSON object.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the depth of the JSON has exceeded the maximum depth of 1000
    /// OR if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteStartObject()
    {
        FlushDeferredPropertyName();
        WriteStart(JsonConstants.OpenBrace);
        _tokenType = JsonTokenType.StartObject;
        SetContainerTypeObject();
    }

    private void WriteStart(byte token)
    {
        if (CurrentDepth >= _maxDepth)
        {
            throw new InvalidOperationException("The JSON depth is exceeds the max allowed depth.");
        }

        if (_indented)
        {
            WriteStartSlow(token);
        }
        else
        {
            WriteStartMinimized(token);
        }

        _currentDepth &= JsonConstants.RemoveFlagsBitMask;
        _currentDepth++;
    }

    private void WriteStartMinimized(byte token)
    {
        const int maxRequired = 2; // 1 start token, and optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        output[bytesWritten++] = token;

        _writer.Advance(bytesWritten);
    }

    private void WriteStartSlow(byte token)
    {
        Debug.Assert(_indented);
        WriteStartIndented(token);
    }

    private void WriteStartIndented(byte token)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);

        var maxRequired = indent + 1 + 1 + _newLineLength; // 1 start token, optionally 1 list separator and 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType is not JsonTokenType.PropertyName and not JsonTokenType.None)
        {
            WriteNewLine(output, ref bytesWritten);
            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        output[bytesWritten++] = token;

        _writer.Advance(bytesWritten);
    }

    /// <summary>
    /// Writes the end of a JSON array.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteEndArray()
    {
        WriteEnd(JsonConstants.CloseBracket);
        _tokenType = JsonTokenType.EndArray;
    }

    /// <summary>
    /// Writes the end of a JSON object.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    public void WriteEndObject()
    {
        WriteEnd(JsonConstants.CloseBrace);
        _tokenType = JsonTokenType.EndObject;
    }

    private void WriteEnd(byte token)
    {
        if (_indented)
        {
            WriteEndSlow(token);
        }
        else
        {
            WriteEndMinimized(token);
        }

        SetFlagToAddListSeparatorBeforeNextItem();
        // Necessary if WriteEndX is called without a corresponding WriteStartX first.
        if (CurrentDepth != 0)
        {
            _currentDepth--;
        }
    }

    private void WriteEndMinimized(byte token)
    {
        var output = _writer.GetSpan(1);
        output[0] = token;
        _writer.Advance(1);
    }

    private void WriteEndSlow(byte token)
    {
        Debug.Assert(_indented);
        WriteEndIndented(token);
    }

    private void WriteEndIndented(byte token)
    {
        // Optionally, write new line and indent.
        // Decrease depth before computing indentation since the end token should be at the previous level.
        var indent = (CurrentDepth > 0 ? CurrentDepth - 1 : 0) * _indentLength;
        Debug.Assert(indent <= _indentLength * _maxDepth);

        var maxRequired = indent + 1 + _newLineLength; // 1 end token, optionally 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_tokenType is not JsonTokenType.StartObject and not JsonTokenType.StartArray)
        {
            WriteNewLine(output, ref bytesWritten);
            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        output[bytesWritten++] = token;

        _writer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteNewLine(Span<byte> output, ref int bytesWritten)
    {
        // Write '\r\n' OR '\n', depending on the configured new line string
        Debug.Assert(_newLineLength is 1 or 2, "Invalid new line length.");
        if (_newLineLength == 2)
        {
            output[bytesWritten++] = JsonConstants.CarriageReturn;
        }
        output[bytesWritten++] = JsonConstants.LineFeed;
    }

    private void WriteIndentation(Span<byte> buffer, int indent)
    {
        Debug.Assert(buffer.Length >= indent);
        var indentByte = _indentByte;

        // Based on perf tests, the break-even point where vectorized Fill is faster
        // than explicitly writing the space in a loop is 8.
        if (indent < 8)
        {
            var i = 0;
            while (i + 1 < indent)
            {
                buffer[i++] = indentByte;
                buffer[i++] = indentByte;
            }

            if (i < indent)
            {
                buffer[i] = indentByte;
            }
        }
        else
        {
            buffer[..indent].Fill(indentByte);
        }
    }

    private void SetFlagToAddListSeparatorBeforeNextItem()
    {
        _currentDepth |= 1 << 31;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TranscodeAndWrite(ReadOnlySpan<char> escapedPropertyName, Span<byte> output, ref int bytesWritten)
    {
        var status = ToUtf8(escapedPropertyName, output[bytesWritten..], out var written);
        Debug.Assert(status == OperationStatus.Done);
        bytesWritten += written;
    }

    private static OperationStatus ToUtf8(ReadOnlySpan<char> source, Span<byte> destination, out int written)
    {
        var status = Utf8.FromUtf16(source, destination, out var charsRead, out written, replaceInvalidSequences: false, isFinalBlock: true);
        Debug.Assert(status is OperationStatus.Done or OperationStatus.DestinationTooSmall or OperationStatus.InvalidData);
        Debug.Assert(charsRead == source.Length || status is not OperationStatus.Done);
        return status;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateValue(ReadOnlySpan<byte> value)
    {
        if (value.Length > JsonConstants.MaxUnescapedTokenSize)
        {
            throw new ArgumentException(
                string.Format(
                    "The JSON value of length {0} is too large and not supported.",
                    value.Length),
                nameof(value));
        }
    }

    /// <summary>
    /// Writes raw UTF-8 JSON bytes directly to the output without any validation or escaping.
    /// </summary>
    /// <param name="utf8Json">The raw UTF-8 encoded JSON to write.</param>
    public void WriteRawValue(ReadOnlySpan<byte> utf8Json)
    {
        FlushDeferredPropertyName();

        var maxRequired = utf8Json.Length + 1; // Optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        utf8Json.CopyTo(output[bytesWritten..]);
        bytesWritten += utf8Json.Length;

        _writer.Advance(bytesWritten);

        SetFlagToAddListSeparatorBeforeNextItem();
    }

    /// <summary>
    /// Internal buffer used for deferred property name writes.
    /// </summary>
    private sealed class DeferBuffer : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[256];
        private int _written;

        public int WrittenCount => _written;

        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        public void Reset() => _written = 0;

        public void Advance(int count) => _written += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_written);
        }

        private void EnsureCapacity(int sizeHint)
        {
            var required = _written + Math.Max(sizeHint, 1);

            if (required > _buffer.Length)
            {
                Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, required));
            }
        }
    }
}
