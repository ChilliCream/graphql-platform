using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HotChocolate.Text.Json;

public sealed partial class JsonWriter
{
    /// <summary>
    /// Writes the property name (as a JSON string) as the first part of a name/value pair of a JSON object.
    /// </summary>
    /// <param name="propertyName">The name of the property to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified property name is too large.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="propertyName"/> parameter is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// The property name is escaped before writing.
    /// </remarks>
    public void WritePropertyName(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        WritePropertyName(propertyName.AsSpan());
    }

    /// <summary>
    /// Writes the property name (as a JSON string) as the first part of a name/value pair of a JSON object.
    /// </summary>
    /// <param name="propertyName">The name of the property to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified property name is too large.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// The property name is escaped before writing.
    /// </remarks>
    public void WritePropertyName(ReadOnlySpan<char> propertyName)
    {
        FlushDeferredPropertyName();

        if (IgnoreNullFields)
        {
            BeginDeferPropertyName();
        }

        ValidateProperty(propertyName);

        var propertyIdx = NeedsEscaping(propertyName, _options.Encoder);

        Debug.Assert(propertyIdx >= -1 && propertyIdx < propertyName.Length && propertyIdx < int.MaxValue / 2);

        if (propertyIdx != -1)
        {
            WriteStringEscapeProperty(propertyName, propertyIdx);
        }
        else
        {
            WriteStringByOptionsPropertyName(propertyName);
        }

        _currentDepth &= JsonConstants.RemoveFlagsBitMask;
        _tokenType = JsonTokenType.PropertyName;
    }

    private void WriteStringEscapeProperty(scoped ReadOnlySpan<char> propertyName, int firstEscapeIndexProp)
    {
        Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= propertyName.Length);

        char[]? propertyArray = null;
        scoped Span<char> escapedPropertyName;

        if (firstEscapeIndexProp != -1)
        {
            var length = GetMaxEscapedLength(propertyName.Length, firstEscapeIndexProp);

            if (length > JsonConstants.StackallocCharThreshold)
            {
                propertyArray = ArrayPool<char>.Shared.Rent(length);
                escapedPropertyName = propertyArray;
            }
            else
            {
                escapedPropertyName = stackalloc char[JsonConstants.StackallocCharThreshold];
            }

            EscapeString(propertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out var written);
            propertyName = escapedPropertyName[..written];
        }

        WriteStringByOptionsPropertyName(propertyName);

        if (propertyArray != null)
        {
            ArrayPool<char>.Shared.Return(propertyArray);
        }
    }

    private void WriteStringByOptionsPropertyName(ReadOnlySpan<char> propertyName)
    {
        if (_indented)
        {
            WriteStringIndentedPropertyName(propertyName);
        }
        else
        {
            WriteStringMinimizedPropertyName(propertyName);
        }
    }

    private void WriteStringMinimizedPropertyName(ReadOnlySpan<char> escapedPropertyName)
    {
        Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
        Debug.Assert(escapedPropertyName.Length < (int.MaxValue - 4) / JsonConstants.MaxExpansionFactorWhileTranscoding);

        // All ASCII, 2 quotes for property name, and 1 colon => escapedPropertyName.Length + 3
        // Optionally, 1 list separator, and up to 3x growth when transcoding
        var maxRequired = (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 4;
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        TranscodeAndWrite(escapedPropertyName, output, ref bytesWritten);

        output[bytesWritten++] = JsonConstants.Quote;
        output[bytesWritten++] = JsonConstants.Colon;

        _writer.Advance(bytesWritten);
    }

    private void WriteStringIndentedPropertyName(ReadOnlySpan<char> escapedPropertyName)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * (_maxDepth));

        Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
        Debug.Assert(escapedPropertyName.Length < (int.MaxValue - 5 - indent - _newLineLength) / JsonConstants.MaxExpansionFactorWhileTranscoding);

        // All ASCII, 2 quotes for property name, 1 colon, and 1 space => escapedPropertyName.Length + 4
        // Optionally, 1 list separator, 1-2 bytes for new line, and up to 3x growth when transcoding
        var maxRequired = indent + (escapedPropertyName.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) + 5 + _newLineLength;
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        if (_tokenType != JsonTokenType.None)
        {
            WriteNewLine(output, ref bytesWritten);
        }

        WriteIndentation(output[bytesWritten..], indent);
        bytesWritten += indent;

        output[bytesWritten++] = JsonConstants.Quote;

        TranscodeAndWrite(escapedPropertyName, output, ref bytesWritten);

        output[bytesWritten++] = JsonConstants.Quote;
        output[bytesWritten++] = JsonConstants.Colon;
        output[bytesWritten++] = JsonConstants.Space;

        _writer.Advance(bytesWritten);
    }

    /// <summary>
    /// Writes the UTF-8 property name (as a JSON string) as the first part of a name/value pair of a JSON object.
    /// </summary>
    /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to write.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the specified property name is too large.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this would result in invalid JSON being written (while validation is enabled).
    /// </exception>
    /// <remarks>
    /// The property name is escaped before writing.
    /// </remarks>
    public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName)
    {
        FlushDeferredPropertyName();

        if (IgnoreNullFields)
        {
            BeginDeferPropertyName();
        }

        ValidateProperty(utf8PropertyName);

        var propertyIdx = NeedsEscaping(utf8PropertyName, _options.Encoder);

        Debug.Assert(propertyIdx >= -1 && propertyIdx < utf8PropertyName.Length && propertyIdx < int.MaxValue / 2);

        if (propertyIdx != -1)
        {
            WriteStringEscapeProperty(utf8PropertyName, propertyIdx);
        }
        else
        {
            WriteStringByOptionsPropertyName(utf8PropertyName);
        }

        _currentDepth &= JsonConstants.RemoveFlagsBitMask;
        _tokenType = JsonTokenType.PropertyName;
    }

    private void WritePropertyNameUnescaped(ReadOnlySpan<byte> utf8PropertyName)
    {
        FlushDeferredPropertyName();

        if (IgnoreNullFields)
        {
            BeginDeferPropertyName();
        }

        ValidateProperty(utf8PropertyName);
        WriteStringByOptionsPropertyName(utf8PropertyName);

        _currentDepth &= JsonConstants.RemoveFlagsBitMask;
        _tokenType = JsonTokenType.PropertyName;
    }

    private void WriteStringEscapeProperty(scoped ReadOnlySpan<byte> utf8PropertyName, int firstEscapeIndexProp)
    {
        Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8PropertyName.Length);

        byte[]? propertyArray = null;
        scoped Span<byte> escapedPropertyName;

        if (firstEscapeIndexProp != -1)
        {
            var length = GetMaxEscapedLength(utf8PropertyName.Length, firstEscapeIndexProp);

            if (length > JsonConstants.StackallocByteThreshold)
            {
                propertyArray = ArrayPool<byte>.Shared.Rent(length);
                escapedPropertyName = propertyArray;
            }
            else
            {
                escapedPropertyName = stackalloc byte[JsonConstants.StackallocByteThreshold];
            }

            EscapeString(utf8PropertyName, escapedPropertyName, firstEscapeIndexProp, _options.Encoder, out var written);
            utf8PropertyName = escapedPropertyName[..written];
        }

        WriteStringByOptionsPropertyName(utf8PropertyName);

        if (propertyArray != null)
        {
            ArrayPool<byte>.Shared.Return(propertyArray);
        }
    }

    private void WriteStringByOptionsPropertyName(ReadOnlySpan<byte> utf8PropertyName)
    {
        if (_indented)
        {
            WriteStringIndentedPropertyName(utf8PropertyName);
        }
        else
        {
            WriteStringMinimizedPropertyName(utf8PropertyName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStringMinimizedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
    {
        Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
        Debug.Assert(escapedPropertyName.Length < int.MaxValue - 4);

        var maxRequired = escapedPropertyName.Length + 4; // 2 quotes, 1 colon, optionally 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        output[bytesWritten++] = JsonConstants.Quote;

        escapedPropertyName.CopyTo(output[bytesWritten..]);
        bytesWritten += escapedPropertyName.Length;

        output[bytesWritten++] = JsonConstants.Quote;
        output[bytesWritten++] = JsonConstants.Colon;

        _writer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStringPropertyNameSection(ReadOnlySpan<byte> escapedPropertyNameSection)
    {
        Debug.Assert(escapedPropertyNameSection.Length <= JsonConstants.MaxEscapedTokenSize - 3);
        Debug.Assert(escapedPropertyNameSection.Length < int.MaxValue - 4);

        var maxRequired = escapedPropertyNameSection.Length + 1; // Optionally, 1 list separator
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        escapedPropertyNameSection.CopyTo(output[bytesWritten..]);
        bytesWritten += escapedPropertyNameSection.Length;

        _writer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStringIndentedPropertyName(ReadOnlySpan<byte> escapedPropertyName)
    {
        var indent = Indentation;
        Debug.Assert(indent <= _indentLength * _maxDepth);

        Debug.Assert(escapedPropertyName.Length <= JsonConstants.MaxEscapedTokenSize);
        Debug.Assert(escapedPropertyName.Length < int.MaxValue - indent - 5 - _newLineLength);

        var maxRequired = indent + escapedPropertyName.Length + 5 + _newLineLength; // 2 quotes, 1 colon, 1 space, optionally 1 list separator and 1-2 bytes for new line
        var bytesWritten = 0;

        var output = _writer.GetSpan(maxRequired);

        if (_currentDepth < 0)
        {
            output[bytesWritten++] = JsonConstants.Comma;
        }

        Debug.Assert(_options.SkipValidation || _tokenType != JsonTokenType.PropertyName);

        if (_tokenType != JsonTokenType.None)
        {
            WriteNewLine(output, ref bytesWritten);
        }

        WriteIndentation(output[bytesWritten..], indent);
        bytesWritten += indent;

        output[bytesWritten++] = JsonConstants.Quote;

        escapedPropertyName.CopyTo(output[bytesWritten..]);
        bytesWritten += escapedPropertyName.Length;

        output[bytesWritten++] = JsonConstants.Quote;
        output[bytesWritten++] = JsonConstants.Colon;
        output[bytesWritten++] = JsonConstants.Space;

        _writer.Advance(bytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateProperty(ReadOnlySpan<byte> propertyName)
    {
        if (propertyName.Length > JsonConstants.MaxUnescapedTokenSize)
        {
            throw new ArgumentException(
                string.Format(
                    "The JSON property name of length {0} is too large and not supported.",
                    propertyName.Length),
                nameof(propertyName));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateProperty(ReadOnlySpan<char> propertyName)
    {
        if (propertyName.Length > JsonConstants.MaxCharacterTokenSize)
        {
            throw new ArgumentException(
                string.Format(
                    "The JSON property name of length {0} is too large and not supported.",
                    propertyName.Length),
                nameof(propertyName));
        }
    }
}
