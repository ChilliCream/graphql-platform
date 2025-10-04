using System.Buffers;
using System.Diagnostics;
using System.Text.Unicode;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal string? GetString(int index, ElementTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index);

        var tokenType = row.TokenType;

        if (tokenType == ElementTokenType.Null)
        {
            return null;
        }

        CheckExpectedType(expectedType, tokenType);

        var segment = ReadRawValue(row);

        if (tokenType is ElementTokenType.String)
        {
            segment = segment[1..^1];
        }

        return row.HasComplexChildren
            ? JsonReaderHelper.GetUnescapedString(segment)
            : JsonReaderHelper.TranscodeHelper(segment);
    }

    internal string GetRequiredString(int index, ElementTokenType expectedType)
    {
        var value = GetString(index, expectedType);

        if (value is null)
        {
            throw new InvalidOperationException("The element value is null.");
        }

        return value;
    }

    internal string GetNameOfPropertyValue(int index)
    {
        // The property name is one row before the property value
        return GetString(index - 1, ElementTokenType.PropertyName)!;
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index - 1);
        Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

        return ReadRawValue(row);
    }

    internal string GetRawValueAsString(int index)
    {
        var segment = GetRawValue(index, includeQuotes: true);
        return JsonReaderHelper.TranscodeHelper(segment.Span);
    }

    internal ReadOnlyMemory<byte> GetRawValue(int index, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index);

        if (row.IsSimpleValue)
        {
            if (includeQuotes && row.TokenType == ElementTokenType.String)
            {
                // Start one character earlier than the value (the open quote)
                // End one character after the value (the close quote)
                return ReadRawValueAsMemory(row);
            }

            var rawValue = ReadRawValueAsMemory(row);
            return rawValue[1..^1];
        }

        // TODO: this is more complex with the new design, we gonna tackle this later.
        // int endElementIdx = GetEndIndex(index, includeEndElement: false);
        // int start = row.Location;
        // row = _parsedData.Get(endElementIdx);
        // return _utf8Json.Slice(start, row.Location - start + row.SizeOrLength);
        throw new NotImplementedException();
    }

    internal string GetPropertyRawValueAsString(int valueIndex)
    {
        var segment = GetPropertyRawValue(valueIndex);
        return JsonReaderHelper.TranscodeHelper(segment.Span);
    }

    private ReadOnlyMemory<byte> GetPropertyRawValue(int valueIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The property name is stored one row before the value
        var row = _metaDb.Get(valueIndex - 1);
        Debug.Assert(row.TokenType == ElementTokenType.PropertyName);

        // Subtract one for the open quote.
        // var start = row.Location - 1;
        // int end;

        row = _metaDb.Get(valueIndex);

        if (row.IsSimpleValue)
        {
            return ReadRawValueAsMemory(row);
        }

        // var endElementIdx = GetEndIndex(valueIndex, includeEndElement: false);
        // row = _parsedData.Get(endElementIdx);
        // end = row.Location + row.SizeOrLength;
        // return _utf8Json.Slice(start, end - start);
        throw new NotImplementedException();
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
            otherText, otherUtf8Text, out var charsRead, out var written,
            replaceInvalidSequences: false, isFinalBlock: true);
        Debug.Assert(status is OperationStatus.Done or
            OperationStatus.DestinationTooSmall or
            OperationStatus.InvalidData);
        Debug.Assert(charsRead == otherText.Length || status is not OperationStatus.Done);

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

        var row = _metaDb.Get(matchIndex);

        CheckExpectedType(
            isPropertyName ? ElementTokenType.PropertyName : ElementTokenType.String,
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
}
