using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Unicode;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal string? GetString(Cursor cursor, JsonTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            // Strings are stored quote-inclusive, so the quoted form is the stored span and the
            // unquoted form slices one byte in on each side.
            if (!includeQuotes && row.TokenType == JsonTokenType.String)
            {
                return ReadRawValue(row.Location, row.SizeOrLength)[1..^1];
            }

            return ReadRawValue(row.Location, row.SizeOrLength);
        }

        var (start, length) = GetCompositeRange(cursor, row);
        return ReadRawValue(start, length);
    }

    internal ReadOnlyMemory<byte> GetRawValueAsMemory(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            if (!includeQuotes && row.TokenType == JsonTokenType.String)
            {
                return ReadRawValueAsMemory(row.Location, row.SizeOrLength)[1..^1];
            }

            return ReadRawValueAsMemory(row.Location, row.SizeOrLength);
        }

        var (start, length) = GetCompositeRange(cursor, row);
        return ReadRawValueAsMemory(start, length);
    }

    internal ValueRange GetRawValuePointer(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(cursor);

        if (row.IsSimpleValue)
        {
            // Strings are stored quote-inclusive, so the quote-inclusive pointer is the stored
            // location and size; the unquoted pointer steps one byte in on each side. Because the
            // value is gap-free, the inner byte sits at the next linear position.
            if (!includeQuotes && row.TokenType == JsonTokenType.String)
            {
                // A single chunk holds the whole value gap-free, so the inner byte is the next
                // offset in the same chunk. Multi-chunk documents follow the chunk schedule, so the
                // inner byte only rolls into the next chunk when the quote ends its chunk.
                int innerStart;
                if (_usedChunks == 1)
                {
                    innerStart = row.Location + 1;
                }
                else
                {
                    var chunk = row.Location >>> DataOffsetBits;
                    var offset = row.Location & DataOffsetMask;
                    innerStart = offset + 1 < GetDataChunkSize(chunk)
                        ? row.Location + 1
                        : EncodeDataLocation(chunk + 1, 0);
                }

                return new ValueRange(innerStart, row.SizeOrLength - 2);
            }

            return new ValueRange(row.Location, row.SizeOrLength);
        }

        var (start, length) = GetCompositeRange(cursor, row);
        return new ValueRange(start, length);
    }

    /// <summary>
    /// Gets the packed start location and byte length of the raw JSON that backs a composite value
    /// (object or array). The length is measured over the gap-free linear positions of the start
    /// and end rows, so no arithmetic is performed on the packed location values.
    /// </summary>
    private (int Start, int Length) GetCompositeRange(Cursor cursor, DbRow row)
    {
        var start = row.Location;
        var endCursor = GetEndIndex(cursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endRowLength = GetEndRowLength(endRow);
        var end = endRow.Location;

        // When start and end share a data chunk the gap-free distance is the plain offset difference,
        // so the linear mapping is only needed when the value spans a chunk boundary.
        var length = (start >>> DataOffsetBits) == (end >>> DataOffsetBits)
            ? (end & DataOffsetMask) - (start & DataOffsetMask) + endRowLength
            : (int)(PackedToLinear(end) - PackedToLinear(start) + endRowLength);
        return (start, length);
    }

    private ReadOnlySpan<byte> GetPropertyRawValue(Cursor valueCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        // The property name is stored one row before the value
        var nameRow = _parsedData.Get(valueCursor - 1);
        Debug.Assert(nameRow.TokenType == JsonTokenType.PropertyName);

        // Property names are stored quote-inclusive, so the open quote is the stored location.
        var start = nameRow.Location;

        var valueRow = _parsedData.Get(valueCursor);

        if (valueRow.IsSimpleValue)
        {
            // Strings and property names are stored quote-inclusive, so the size already covers the
            // quotes; numbers and literals end at their stored length. When the name and value share
            // a data chunk the distance is the plain offset difference, otherwise map to linear.
            var end = valueRow.Location;
            var length = (start >>> DataOffsetBits) == (end >>> DataOffsetBits)
                ? (end & DataOffsetMask) - (start & DataOffsetMask) + valueRow.SizeOrLength
                : (int)(PackedToLinear(end) + valueRow.SizeOrLength - PackedToLinear(start));
            return ReadRawValue(start, length);
        }

        var endCursor = GetEndIndex(valueCursor, includeEndElement: false);
        var endRow = _parsedData.Get(endCursor);
        var endRowLength = GetEndRowLength(endRow);
        var endLocation = endRow.Location;

        var compositeLength = (start >>> DataOffsetBits) == (endLocation >>> DataOffsetBits)
            ? (endLocation & DataOffsetMask) - (start & DataOffsetMask) + endRowLength
            : (int)(PackedToLinear(endLocation) + endRowLength - PackedToLinear(start));
        return ReadRawValue(start, compositeLength);
    }

    internal Cursor GetEndIndex(Cursor cursor, bool includeEndElement)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DbRow GetDbRow(Cursor cursor) => _parsedData.Get(cursor);

    // Reads the value row once with the same disposed guard the value accessors use, so callers
    // can derive the token type and value range from a single row read.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DbRow GetValueRow(Cursor cursor) => _parsedData.Get(cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetEndRowLength(DbRow endRow)
        => endRow.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray
            ? 1
            : endRow.SizeOrLength;
}
