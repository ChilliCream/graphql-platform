using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal bool TryGetNamedPropertyValue(
        Cursor objectCursor,
        ReadOnlySpan<char> propertyName,
        out SourceResultElement value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(objectCursor);

        CheckExpectedType(JsonTokenType.StartObject, row.TokenType);

        // Only one row means it was EndObject.
        if (row.NumberOfRows == 1)
        {
            value = default;
            return false;
        }

        var maxBytes = s_utf8Encoding.GetMaxByteCount(propertyName.Length);

        if (maxBytes < JsonConstants.StackallocByteThreshold)
        {
            Span<byte> utf8Name = stackalloc byte[JsonConstants.StackallocByteThreshold];
            var len = s_utf8Encoding.GetBytes(propertyName, utf8Name);
            utf8Name = utf8Name[..len];

            return TryGetNamedPropertyValueCore(
                objectCursor,
                objectCursor + (row.NumberOfRows - 1), // endId (EndObject row)
                utf8Name,
                out value);
        }

        // Unescaping the property name will make the string shorter (or the same)
        // So the first viable candidate is one whose length in bytes matches, or
        // exceeds, our length in chars.
        //
        // The maximal escaping seems to be 6 -> 1 ("\u0030" => "0"), but just transcode
        // and switch once one viable long property is found.

        var minBytes = propertyName.Length;

        // Move to the row before the EndObject
        var endCursor = objectCursor + (row.NumberOfRows - 1);
        var candidateCursor = endCursor - 1;

        while (candidateCursor > objectCursor)
        {
            var passedCursor = candidateCursor;
            var candidateRow = _parsedData.Get(candidateCursor);
            Debug.Assert(candidateRow.TokenType != JsonTokenType.PropertyName);

            // Move before the value
            candidateCursor -= candidateRow.IsSimpleValue ? 1 : candidateRow.NumberOfRows;
            candidateRow = _parsedData.Get(candidateCursor);
            Debug.Assert(candidateRow.TokenType == JsonTokenType.PropertyName);

            if (candidateRow.SizeOrLength >= minBytes)
            {
                var tmpUtf8 = ArrayPool<byte>.Shared.Rent(maxBytes);
                Span<byte> utf8Name = default;

                try
                {
                    var len = s_utf8Encoding.GetBytes(propertyName, tmpUtf8);
                    utf8Name = tmpUtf8.AsSpan(0, len);

                    return TryGetNamedPropertyValueCore(
                        objectCursor + 1,
                        passedCursor + 1,
                        utf8Name,
                        out value);
                }
                finally
                {
                    utf8Name.Clear();
                    ArrayPool<byte>.Shared.Return(tmpUtf8);
                }
            }

            // Move to previous value
            candidateCursor -= 1;
        }

        // None of the property names were within the range that the UTF-8 encoding would have been.
        value = default;
        return false;
    }

    internal bool TryGetNamedPropertyValue(
        Cursor objectCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _parsedData.Get(objectCursor);

        CheckExpectedType(JsonTokenType.StartObject, row.TokenType);

        // Only one row means it was EndObject.
        if (row.NumberOfRows == 1)
        {
            value = default;
            return false;
        }

        var startCursor = objectCursor + 1;
        var endCursor = objectCursor + (row.NumberOfRows - 1);

        return TryGetNamedPropertyValueCore(
            startCursor,
            endCursor,
            propertyName,
            out value);
    }

    private bool TryGetNamedPropertyValueCore(
        Cursor startCursor,
        Cursor endCursor,
        ReadOnlySpan<byte> propertyName,
        out SourceResultElement value)
    {
        Span<byte> utf8UnescapedStack = stackalloc byte[JsonConstants.StackallocByteThreshold];

        // Move to the row before the EndObject
        var cursor = endCursor - 1;

        while (cursor > startCursor)
        {
            var row = _parsedData.Get(cursor);
            Debug.Assert(row.TokenType != JsonTokenType.PropertyName);

            // Move before the value
            cursor -= row.IsSimpleValue ? 1 : row.NumberOfRows;

            row = _parsedData.Get(cursor);
            Debug.Assert(row.TokenType == JsonTokenType.PropertyName);

            var currentPropertyName = ReadRawValue(row, includeQuotes: false);

            if (row.HasComplexChildren)
            {
                // An escaped property name will be longer than an unescaped candidate, so only unescape
                // when the lengths are compatible.
                if (currentPropertyName.Length > propertyName.Length)
                {
                    var idx = currentPropertyName.IndexOf(JsonConstants.BackSlash);
                    Debug.Assert(idx >= 0);

                    // If everything up to where the property name has a backslash matches, keep going.
                    if (propertyName.Length > idx
                        && currentPropertyName[..idx].SequenceEqual(propertyName[..idx]))
                    {
                        var remaining = currentPropertyName.Length - idx;
                        var written = 0;
                        byte[]? rented = null;

                        try
                        {
                            var utf8Unescaped =
                                remaining <= utf8UnescapedStack.Length
                                    ? utf8UnescapedStack
                                    : (rented = ArrayPool<byte>.Shared.Rent(remaining));

                            // Only unescape the part we haven't processed.
                            JsonReaderHelper.Unescape(currentPropertyName[idx..], utf8Unescaped, 0, out written);

                            // If the unescaped remainder matches the input remainder, it's a match.
                            if (utf8Unescaped[..written].SequenceEqual(propertyName[idx..]))
                            {
                                // If the property name is a match, the answer is the next element.
                                value = new SourceResultElement(this, cursor + 1);
                                return true;
                            }
                        }
                        finally
                        {
                            if (rented is not null)
                            {
                                rented.AsSpan(0, written).Clear();
                                ArrayPool<byte>.Shared.Return(rented);
                            }
                        }
                    }
                }
            }
            else if (currentPropertyName.SequenceEqual(propertyName))
            {
                // If the property name is a match, the answer is the next element.
                value = new SourceResultElement(this, cursor + 1);
                return true;
            }

            // Move to the previous value (name row is at 'id', previous value ends at id - 1)
            cursor -= 1;
        }

        value = default;
        return false;
    }
}
