using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Unicode;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal string? GetString(Cursor cursor, JsonTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);
        var rowTokenType = row.TokenType;

        if (rowTokenType is JsonTokenType.Null)
        {
            return null;
        }

        CheckExpectedType(expectedType, rowTokenType);

        var segment = ReadRawValue(row, includeQuotes: false);

        return row.HasComplexChildren
            ? JsonReaderHelper.GetUnescapedString(segment)
            : JsonReaderHelper.TranscodeHelper(segment);
    }

    internal bool TextEquals(Cursor cursor, ReadOnlySpan<char> otherText, bool isPropertyName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[]? otherUtf8TextArray = null;
        var length = checked(otherText.Length * JsonConstants.MaxExpansionFactorWhileTranscoding);
        var otherUtf8Text = length <= JsonConstants.StackallocByteThreshold
            ? stackalloc byte[JsonConstants.StackallocByteThreshold]
            : (otherUtf8TextArray = ArrayPool<byte>.Shared.Rent(length));

        var status = Utf8.FromUtf16(
            otherText,
            otherUtf8Text,
            out var charsRead,
            out var written,
            replaceInvalidSequences: false,
            isFinalBlock: true);

        Debug.Assert(status is OperationStatus.Done or OperationStatus.DestinationTooSmall or OperationStatus.InvalidData);
        Debug.Assert(charsRead == otherText.Length || status is not OperationStatus.Done);
        Debug.Assert(status != OperationStatus.DestinationTooSmall);

        bool result;
        if (status == OperationStatus.InvalidData)
        {
            result = false;
        }
        else
        {
            Debug.Assert(status == OperationStatus.Done);
            result = TextEquals(cursor, otherUtf8Text[..written], isPropertyName, shouldUnescape: true);
        }

        if (otherUtf8TextArray is not null)
        {
            otherUtf8Text[..written].Clear();
            ArrayPool<byte>.Shared.Return(otherUtf8TextArray);
        }

        return result;
    }

    internal bool TextEquals(Cursor cursor, ReadOnlySpan<byte> otherUtf8Text, bool isPropertyName, bool shouldUnescape)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The propertyName is stored exactly one row before its value
        var matchCursor = isPropertyName ? cursor - 1 : cursor;

        var row = _parsedData.Get(matchCursor);

        CheckExpectedType(isPropertyName ? JsonTokenType.PropertyName : JsonTokenType.String, row.TokenType);

        var segment = ReadRawValue(row, includeQuotes: false);

        if (otherUtf8Text.Length > segment.Length || (!shouldUnescape && otherUtf8Text.Length != segment.Length))
        {
            return false;
        }

        if (row.HasComplexChildren && shouldUnescape)
        {
            if (otherUtf8Text.Length < segment.Length / JsonConstants.MaxExpansionFactorWhileEscaping)
            {
                return false;
            }

            var idx = segment.IndexOf(JsonConstants.BackSlash);
            Debug.Assert(idx != -1);

            if (!otherUtf8Text.StartsWith(segment[..idx]))
            {
                return false;
            }

            return JsonReaderHelper.UnescapeAndCompare(segment[idx..], otherUtf8Text[idx..]);
        }

        return segment.SequenceEqual(otherUtf8Text);
    }

    internal string GetNameOfPropertyValue(Cursor valueCursor)
        // The propertyName is stored exactly one row before its value
        => GetString(valueCursor - 1, JsonTokenType.PropertyName)!;

    internal ReadOnlySpan<byte> GetPropertyNameRaw(Cursor valueCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(valueCursor - 1);
        Debug.Assert(row.TokenType is JsonTokenType.PropertyName);

        return ReadRawValue(row, includeQuotes: false);
    }

    internal string GetRawValueAsString(Cursor cursor)
    {
        var segment = GetRawValue(cursor, includeQuotes: true);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    internal string GetPropertyRawValueAsString(Cursor valueCursor)
    {
        var segment = GetPropertyRawValue(valueCursor);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    internal ReadOnlySpan<byte> GetRawValue(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            if (includeQuotes && row.TokenType == JsonTokenType.String)
            {
                // Start one character earlier than the value (the open quote)
                // End one character after the value (the close quote)
                return ReadRawValue(row.Location - 1, row.SizeOrLength + 2);
            }

            return ReadRawValue(row, includeQuotes: false);
        }

        var start = row.Location;
        var endCursor = GetEndIndex(cursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endRowLength = endRow.SizeOrLength;

        if (endRow.TokenType is JsonTokenType.EndObject or JsonTokenType.StartArray)
        {
            endRowLength = 1;
        }

        return ReadRawValue(start, endRow.Location - start + endRowLength);
    }

    internal ReadOnlyMemory<byte> GetRawValueAsMemory(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            if (includeQuotes && row.TokenType == JsonTokenType.String)
            {
                // Start one character earlier than the value (the open quote)
                // End one character after the value (the close quote)
                return ReadRawValueAsMemory(row.Location - 1, row.SizeOrLength + 2);
            }

            return ReadRawValueAsMemory(row.Location, row.SizeOrLength);
        }

        var start = row.Location;
        var endCursor = GetEndIndex(cursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endRowLength = endRow.SizeOrLength;

        if (endRow.TokenType is JsonTokenType.EndObject or JsonTokenType.StartArray)
        {
            endRowLength = 1;
        }

        return ReadRawValueAsMemory(start, endRow.Location - start + endRowLength);
    }

    internal ValueRange GetRawValuePointer(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            if (includeQuotes && row.TokenType == JsonTokenType.String)
            {
                // Start one character earlier than the value (the open quote)
                // End one character after the value (the close quote)
                return new ValueRange(row.Location - 1, row.SizeOrLength + 2);
            }

            return new ValueRange(row.Location, row.SizeOrLength);
        }

        var start = row.Location;
        var endCursor = GetEndIndex(cursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endRowLength = endRow.SizeOrLength;

        if (endRow.TokenType is JsonTokenType.EndObject or JsonTokenType.StartArray)
        {
            endRowLength = 1;
        }

        return new ValueRange(start, endRow.Location - start + endRowLength);
    }

    private ReadOnlySpan<byte> GetPropertyRawValue(Cursor valueCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The property name is stored one row before the value
        var nameRow = _parsedData.Get(valueCursor - 1);
        Debug.Assert(nameRow.TokenType == JsonTokenType.PropertyName);

        // Subtract one for the open quote.
        var start = nameRow.Location - 1;

        var valueRow = _parsedData.Get(valueCursor);

        if (valueRow.IsSimpleValue)
        {
            var end = valueRow.Location + valueRow.SizeOrLength;

            if (valueRow.TokenType == JsonTokenType.String)
            {
                end++; // include closing quote for strings
            }

            return ReadRawValue(start, end - start);
        }

        var endCursor = GetEndIndex(valueCursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endOffset = endRow.Location + endRow.SizeOrLength;
        return ReadRawValue(start, endOffset - start);
    }

    internal Cursor GetEndIndex(Cursor cursor, bool includeEndElement)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            // End index is the row after a simple value
            return cursor + 1;
        }

        // Last row within this composite = start + (rows - 1)
        var endId = cursor + (row.NumberOfRows - 1);

        if (includeEndElement)
        {
            endId += 1;
        }

        return endId;
    }
}
