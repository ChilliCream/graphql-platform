using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Unicode;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal string? GetString(int index, JsonTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        var tokenType = row.TokenType;

        if (tokenType == JsonTokenType.Null)
        {
            return null;
        }

        CheckExpectedType(expectedType, tokenType);

        var segment = ReadRawValue(row);

        return row.HasComplexChildren
            ? JsonReaderHelper.GetUnescapedString(segment)
            : JsonReaderHelper.TranscodeHelper(segment);
    }

    internal bool TextEquals(int index, ReadOnlySpan<char> otherText, bool isPropertyName)
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
            result = TextEquals(index, otherUtf8Text[..written], isPropertyName, shouldUnescape: true);
        }

        if (otherUtf8TextArray != null)
        {
            otherUtf8Text[..written].Clear();
            ArrayPool<byte>.Shared.Return(otherUtf8TextArray);
        }

        return result;
    }

    internal bool TextEquals(int index, ReadOnlySpan<byte> otherUtf8Text, bool isPropertyName, bool shouldUnescape)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var matchIndex = isPropertyName ? index - DbRow.Size : index;

        var row = _parsedData.Get(matchIndex);

        CheckExpectedType(
            isPropertyName ? JsonTokenType.PropertyName : JsonTokenType.String,
            row.TokenType);

        var segment = ReadRawValue(row);

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

    internal string GetNameOfPropertyValue(int index)
    {
        // The property name is one row before the property value
        return GetString(index - DbRow.Size, JsonTokenType.PropertyName)!;
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index - DbRow.Size);
        Debug.Assert(row.TokenType is JsonTokenType.PropertyName);

        return ReadRawValue(row);
    }

    internal string GetRawValueAsString(int index)
    {
        var segment = GetRawValue(index, includeQuotes: true);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    internal string GetPropertyRawValueAsString(int valueIndex)
    {
        var segment = GetPropertyRawValue(valueIndex);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    internal ReadOnlySpan<byte> GetRawValue(int index, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        if (row.IsSimpleValue)
        {
            if (includeQuotes && row.TokenType == JsonTokenType.String)
            {
                // Start one character earlier than the value (the open quote)
                // End one character after the value (the close quote)
                return ReadRawValue(row.Location - 1, row.SizeOrLength + 2);
            }

            return ReadRawValue(row);
        }

        var endElementIdx = GetEndIndex(index, includeEndElement: false);
        var start = row.Location;
        row = _parsedData.Get(endElementIdx);
        return ReadRawValue(start, row.Location - start + row.SizeOrLength);
    }

    internal ValueRange GetRawValuePointer(int index, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

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

        var endElementIdx = GetEndIndex(index, includeEndElement: false);
        var start = row.Location;
        row = _parsedData.Get(endElementIdx);
        return new ValueRange(start, row.Location - start + row.SizeOrLength);
    }

    private ReadOnlySpan<byte> GetPropertyRawValue(int valueIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The property name is stored one row before the value
        var row = _parsedData.Get(valueIndex - DbRow.Size);
        Debug.Assert(row.TokenType == JsonTokenType.PropertyName);

        // Subtract one for the open quote.
        var start = row.Location - 1;
        int end;

        row = _parsedData.Get(valueIndex);

        if (row.IsSimpleValue)
        {
            end = row.Location + row.SizeOrLength;

            // If the value was a string, pick up the terminating quote.
            if (row.TokenType == JsonTokenType.String)
            {
                end++;
            }

            return ReadRawValue(start, end - start);
        }

        var endElementIdx = GetEndIndex(valueIndex, includeEndElement: false);
        row = _parsedData.Get(endElementIdx);
        end = row.Location + row.SizeOrLength;
        return ReadRawValue(start, end - start);
    }

    internal int GetEndIndex(int index, bool includeEndElement)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(index);

        if (row.IsSimpleValue)
        {
            return index + DbRow.Size;
        }

        var endIndex = index + DbRow.Size * row.NumberOfRows;

        if (includeEndElement)
        {
            endIndex += DbRow.Size;
        }

        return endIndex;
    }
}
