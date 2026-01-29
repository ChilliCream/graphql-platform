using System.Buffers;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    /// <summary>
    /// Writes the string text value (as a JSON string) as an element of a JSON array.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified value is too large.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// <para>
    /// The value is escaped before writing.</para>
    /// <para>
    /// If <paramref name="value"/> is <see langword="null"/> the JSON null value is written,
    /// as if <see cref="WriteNullValue"/> was called.
    /// </para>
    /// </remarks>
    public void WriteStringValue(string? value)
    {
        if (value == null)
        {
            WriteNullValue();
        }
        else
        {
            WriteStringValue(value.AsSpan());
        }
    }

    /// <summary>
    /// Writes the text value (as a JSON string) as an element of a JSON array.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified value is too large.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// The value is escaped before writing.
    /// </remarks>
    public void WriteStringValue(ReadOnlySpan<char> value)
    {
        WriteStringEscape(value);

        SetFlagToAddListSeparatorBeforeNextItem();
        _tokenType = JsonTokenType.String;
    }

    private void WriteStringEscape(ReadOnlySpan<char> value)
    {
        var valueIdx = NeedsEscaping(value, _options.Encoder);

        Debug.Assert(valueIdx >= -1 && valueIdx < value.Length);

        if (valueIdx != -1)
        {
            WriteStringEscapeValue(value, valueIdx);
        }
        else
        {
            WriteStringByOptions(value);
        }
    }

    private void WriteStringByOptions(ReadOnlySpan<char> value)
    {
        if (_indented)
        {
            WriteStringIndented(value);
        }
        else
        {
            WriteStringMinimized(value);
        }
    }

    private void WriteStringMinimized(ReadOnlySpan<char> escapedValue)
    {
        Debug.Assert(escapedValue.Length < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - 3);

        // All ASCII, 2 quotes => escapedValue.Length + 2
        // Optionally, 1 list separator, and up to 3x growth when transcoding
        var maxRequired = (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 3;
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        TranscodeAndWrite(escapedValue, output, ref bytesWritten);

        output[bytesWritten++] = JsonConstants.Quote;

        _writer.Advance(bytesWritten);
    }

    // TODO: https://github.com/dotnet/runtime/issues/29293
    private void WriteStringIndented(ReadOnlySpan<char> escapedValue)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);
        Debug.Assert(escapedValue.Length
            < (int.MaxValue / JsonConstants.MaxExpansionFactorWhileTranscoding) - indent - 3 - _newLineLength);

        // All ASCII, 2 quotes => indent + escapedValue.Length + 2
        // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
        var maxRequired = indent
            + (escapedValue.Length * JsonConstants.MaxExpansionFactorWhileTranscoding)
            + 3
            + _newLineLength;
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType != JsonTokenType.PropertyName)
        {
            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output, ref bytesWritten);
            }

            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        TranscodeAndWrite(escapedValue, output, ref bytesWritten);

        output[bytesWritten++] = JsonConstants.Quote;

        _writer.Advance(bytesWritten);
    }

    private void WriteStringEscapeValue(ReadOnlySpan<char> value, int firstEscapeIndexVal)
    {
        Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= value.Length);
        Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < value.Length);

        char[]? valueArray = null;

        var length = GetMaxEscapedLength(value.Length, firstEscapeIndexVal);

        var escapedValue = length <= JsonConstants.StackallocCharThreshold
            ? stackalloc char[JsonConstants.StackallocCharThreshold]
            : (valueArray = ArrayPool<char>.Shared.Rent(length));

        EscapeString(value, escapedValue, firstEscapeIndexVal, _options.Encoder, out var written);

        WriteStringByOptions(escapedValue[..written]);

        if (valueArray != null)
        {
            ArrayPool<char>.Shared.Return(valueArray);
        }
    }

    /// <summary>
    /// Writes the UTF-8 text value (as a JSON string) as an element of a JSON array.
    /// </summary>
    /// <param name="utf8Value">The UTF-8 encoded value to be written as a JSON string element of a JSON array.</param>
    /// <param name="skipEscaping">If true, the value is assumed to be already escaped and will be written directly.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified value is too large.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// The value is escaped before writing unless skipEscaping is true.
    /// </remarks>
    public void WriteStringValue(ReadOnlySpan<byte> utf8Value, bool skipEscaping = false)
    {
        ValidateValue(utf8Value);

        if (skipEscaping)
        {
            // Value is already escaped and includes quotes, write directly without adding quotes
            WriteStringValueRaw(utf8Value);
        }
        else
        {
            WriteStringEscape(utf8Value);
        }

        SetFlagToAddListSeparatorBeforeNextItem();
        _tokenType = JsonTokenType.String;
    }

    private void WriteStringValueRaw(ReadOnlySpan<byte> utf8Value)
    {
        if (_indented)
        {
            WriteStringValueRawIndented(utf8Value);
        }
        else
        {
            WriteStringValueRawMinimized(utf8Value);
        }
    }

    private void WriteStringValueRawMinimized(ReadOnlySpan<byte> utf8Value)
    {
        var maxRequired = utf8Value.Length + 1; // Optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        utf8Value.CopyTo(output[bytesWritten..]);
        bytesWritten += utf8Value.Length;

        _writer.Advance(bytesWritten);
    }

    private void WriteStringValueRawIndented(ReadOnlySpan<byte> utf8Value)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);

        var maxRequired = indent + utf8Value.Length + 1 + _newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType != JsonTokenType.PropertyName)
        {
            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output, ref bytesWritten);
            }

            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        utf8Value.CopyTo(output[bytesWritten..]);
        bytesWritten += utf8Value.Length;

        _writer.Advance(bytesWritten);
    }

    private void WriteStringEscape(ReadOnlySpan<byte> utf8Value)
    {
        var valueIdx = NeedsEscaping(utf8Value, _options.Encoder);

        Debug.Assert(valueIdx >= -1 && valueIdx < utf8Value.Length);

        if (valueIdx != -1)
        {
            WriteStringEscapeValue(utf8Value, valueIdx);
        }
        else
        {
            WriteStringByOptions(utf8Value);
        }
    }

    private void WriteStringByOptions(ReadOnlySpan<byte> utf8Value)
    {
        if (_indented)
        {
            WriteStringIndented(utf8Value);
        }
        else
        {
            WriteStringMinimized(utf8Value);
        }
    }

    // TODO: https://github.com/dotnet/runtime/issues/29293
    private void WriteStringMinimized(ReadOnlySpan<byte> escapedValue)
    {
        Debug.Assert(escapedValue.Length < int.MaxValue - 3);

        var minRequired = escapedValue.Length + 2; // 2 quotes
        var maxRequired = minRequired + 1; // Optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        escapedValue.CopyTo(output[bytesWritten..]);
        bytesWritten += escapedValue.Length;

        output[bytesWritten++] = JsonConstants.Quote;

        _writer.Advance(bytesWritten);
    }

    // TODO: https://github.com/dotnet/runtime/issues/29293
    private void WriteStringIndented(ReadOnlySpan<byte> escapedValue)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);
        Debug.Assert(escapedValue.Length < int.MaxValue - indent - 3 - _newLineLength);

        var minRequired = indent + escapedValue.Length + 2; // 2 quotes
        var maxRequired = minRequired + 1 + _newLineLength; // Optionally, 1 list separator and 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType != JsonTokenType.PropertyName)
        {
            if (_tokenType != JsonTokenType.None)
            {
                WriteNewLine(output, ref bytesWritten);
            }

            WriteIndentation(output[bytesWritten..], indent);
            bytesWritten += indent;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        escapedValue.CopyTo(output[bytesWritten..]);
        bytesWritten += escapedValue.Length;

        output[bytesWritten++] = JsonConstants.Quote;

        _writer.Advance(bytesWritten);
    }

    private void WriteStringEscapeValue(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
    {
        Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
        Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

        byte[]? valueArray = null;

        var length = GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

        var escapedValue = length <= JsonConstants.StackallocByteThreshold
            ? stackalloc byte[JsonConstants.StackallocByteThreshold]
            : (valueArray = ArrayPool<byte>.Shared.Rent(length));

        EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, _options.Encoder, out var written);

        WriteStringByOptions(escapedValue[..written]);

        if (valueArray != null)
        {
            ArrayPool<byte>.Shared.Return(valueArray);
        }
    }

    /// <summary>
    /// Writes a number as a JSON string. The string value is not escaped.
    /// </summary>
    /// <param name="utf8Value"></param>
    internal void WriteNumberValueAsStringUnescaped(ReadOnlySpan<byte> utf8Value)
    {
        // The value has been validated prior to calling this method.

        WriteStringByOptions(utf8Value);

        SetFlagToAddListSeparatorBeforeNextItem();
        _tokenType = JsonTokenType.String;
    }
}
