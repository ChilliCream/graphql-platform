using System.Buffers;
using System.Diagnostics;
using System.Text.Unicode;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal string? GetString(Cursor cursor, ElementTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);
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

    internal string GetRequiredString(Cursor cursor, ElementTokenType expectedType)
    {
        var value = GetString(cursor, expectedType);

        if (value is null)
        {
            throw new InvalidOperationException("The element value is null.");
        }

        return value;
    }

    internal string GetNameOfPropertyValue(Cursor valueCursor)
    {
        // The property name is one row before the property value
        return GetString(valueCursor + (-1), ElementTokenType.PropertyName)!;
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw(Cursor valueCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The property name is stored one row before the value
        var nameCursor = valueCursor + (-1);
        var row = _metaDb.Get(nameCursor);
        Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

        return ReadRawValue(row);
    }

    internal string GetRawValueAsString(Cursor cursor)
    {
        var segment = GetRawValue(cursor, includeQuotes: true);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    internal ReadOnlySpan<byte> GetRawValue(Cursor cursor, bool includeQuotes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(cursor);

        if (row.IsSimpleValue)
        {
            if (!includeQuotes && row.TokenType == ElementTokenType.String)
            {
                // Skip opening/closing quotes
                return ReadRawValue(row)[1..^1];
            }

            return ReadRawValue(row);
        }

        // TODO: this is more complex with the new design, we gonna tackle this later.
        // var endCursor = GetEndCursor(cursor, includeEndElement: false);
        // var start = row.Location;
        // var endRow = _metaDb.Get(endCursor);
        // return _utf8Json.Slice(start, endRow.Location - start + endRow.SizeOrLength);
        throw new NotImplementedException();
    }

    internal string GetPropertyRawValueAsString(Cursor valueCursor)
    {
        var segment = GetPropertyRawValue(valueCursor);
        return JsonReaderHelper.TranscodeHelper(segment);
    }

    private ReadOnlySpan<byte> GetPropertyRawValue(Cursor valueCursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The property name is stored one row before the value
        Debug.Assert(_metaDb.GetElementTokenType(valueCursor - 1) == ElementTokenType.PropertyName);

        var row = _metaDb.Get(valueCursor);

        if (row.IsSimpleValue)
        {
            return ReadRawValue(row);
        }

        // var endCursor = GetEndCursor(valueCursor, includeEndElement: false);
        // var endRow = _metaDb.Get(endCursor);
        // return _utf8Json.Slice(start, end - start);
        throw new NotSupportedException("Properties are expected to be simple values.");
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
            result = TextEquals(cursor, otherUtf8Text[..written], isPropertyName, shouldUnescape: true);
        }

        if (otherUtf8TextArray != null)
        {
            otherUtf8Text[..written].Clear();
            ArrayPool<byte>.Shared.Return(otherUtf8TextArray);
        }

        return result;
    }

    internal bool TextEquals(Cursor cursor, ReadOnlySpan<byte> otherUtf8Text, bool isPropertyName, bool shouldUnescape)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var matchCursor = isPropertyName ? cursor + (-1) : cursor;
        var row = _metaDb.Get(matchCursor);

        CheckExpectedType(
            isPropertyName ? ElementTokenType.PropertyName : ElementTokenType.String,
            row.TokenType);

        var segment = ReadRawValue(row);

        if (!isPropertyName)
        {
            segment = segment[1..^1];
        }

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
