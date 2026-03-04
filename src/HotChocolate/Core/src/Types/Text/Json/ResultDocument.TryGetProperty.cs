using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    internal bool TryGetNamedPropertyValue(
        Cursor startCursor,
        string propertyName,
        out ResultElement value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (startCursor, var tokenType) = _metaDb.GetStartCursor(startCursor);
        CheckExpectedType(ElementTokenType.StartObject, tokenType);

        var numberOfRows = _metaDb.GetNumberOfRows(startCursor);

        // Only one row means it was EndObject.
        if (numberOfRows == 1)
        {
            value = default;
            return false;
        }

        var row = _metaDb.Get(startCursor);
        if (row.OperationReferenceType is OperationReferenceType.SelectionSet)
        {
            var selectionSet = _operation.GetSelectionSetById(row.OperationReferenceId);
            if (selectionSet.TryGetSelection(propertyName, out var selection))
            {
                var propertyIndex = selection.Id - selectionSet.Id - 1;
                var propertyRowIndex = (propertyIndex * 2) + 1;
                var propertyCursor = startCursor + propertyRowIndex;
                Debug.Assert(_metaDb.GetElementTokenType(propertyCursor) is ElementTokenType.PropertyName);
                Debug.Assert(_metaDb.Get(propertyCursor).OperationReferenceId == selection.Id);
                value = new ResultElement(this, propertyCursor + 1);
                return true;
            }
        }

        var maxBytes = s_utf8Encoding.GetMaxByteCount(propertyName.Length);
        var endCursor = startCursor + (numberOfRows - 1);

        if (maxBytes < JsonConstants.StackallocByteThreshold)
        {
            Span<byte> utf8Name = stackalloc byte[JsonConstants.StackallocByteThreshold];
            var len = s_utf8Encoding.GetBytes(propertyName, utf8Name);
            utf8Name = utf8Name[..len];

            return TryGetNamedPropertyValue(
                startCursor + 1,
                endCursor,
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
        var candidate = endCursor;

        while (candidate > startCursor)
        {
            var passed = candidate;

            row = _metaDb.Get(candidate);
            Debug.Assert(row.TokenType != ElementTokenType.PropertyName);

            candidate--;
            row = _metaDb.Get(candidate);
            Debug.Assert(row.TokenType == ElementTokenType.PropertyName);

            if (row.SizeOrLength >= minBytes)
            {
                var tmpUtf8 = ArrayPool<byte>.Shared.Rent(maxBytes);
                Span<byte> utf8Name = default;

                try
                {
                    var len = s_utf8Encoding.GetBytes(propertyName, tmpUtf8);
                    utf8Name = tmpUtf8.AsSpan(0, len);

                    return TryGetNamedPropertyValue(
                        startCursor,
                        passed + 1,
                        utf8Name,
                        out value);
                }
                finally
                {
                    // While property names aren't usually a secret, they also usually
                    // aren't long enough to end up in the rented buffer transcode path.
                    //
                    // On the basis that this is user data, go ahead and clear it.
                    utf8Name.Clear();
                    ArrayPool<byte>.Shared.Return(tmpUtf8);
                }
            }

            // Move to the previous value
            candidate--;
        }

        // None of the property names were within the range that the UTF-8 encoding would have been.
        value = default;
        return false;
    }

    internal bool TryGetNamedPropertyValue(
        Cursor startCursor,
        ReadOnlySpan<byte> propertyName,
        out ResultElement value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (startCursor, var tokenType) = _metaDb.GetStartCursor(startCursor);
        CheckExpectedType(ElementTokenType.StartObject, tokenType);

        var numberOfRows = _metaDb.GetNumberOfRows(startCursor);

        // Only one row means it was EndObject.
        if (numberOfRows == 1)
        {
            value = default;
            return false;
        }

        var row = _metaDb.Get(startCursor);
        if (row.OperationReferenceType is OperationReferenceType.SelectionSet)
        {
            var selectionSet = _operation.GetSelectionSetById(row.OperationReferenceId);
            if (selectionSet.TryGetSelection(propertyName, out var selection))
            {
                var propertyIndex = selection.Id - selectionSet.Id - 1;
                var propertyRowIndex = (propertyIndex * 2) + 1;
                var propertyCursor = startCursor + propertyRowIndex;
                Debug.Assert(_metaDb.GetElementTokenType(propertyCursor) is ElementTokenType.PropertyName);
                Debug.Assert(_metaDb.Get(propertyCursor).OperationReferenceId == selection.Id);
                value = new ResultElement(this, propertyCursor + 1);
                return true;
            }
        }

        var endCursor = startCursor + (numberOfRows - 1);

        return TryGetNamedPropertyValue(
            startCursor + 1,
            endCursor,
            propertyName,
            out value);
    }

    private bool TryGetNamedPropertyValue(
        Cursor startCursor,
        Cursor endCursor,
        ReadOnlySpan<byte> propertyName,
        out ResultElement value)
    {
        Span<byte> utf8UnescapedStack = stackalloc byte[JsonConstants.StackallocByteThreshold];
        var cursor = endCursor;

        while (cursor > startCursor)
        {
            var row = _metaDb.Get(cursor);
            Debug.Assert(row.TokenType != ElementTokenType.PropertyName);
            cursor--;

            row = _metaDb.Get(cursor);
            Debug.Assert(row.TokenType == ElementTokenType.PropertyName);
            var currentPropertyName = ReadRawValue(row);

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
                            var utf8Unescaped = remaining <= utf8UnescapedStack.Length
                                ? utf8UnescapedStack
                                : (rented = ArrayPool<byte>.Shared.Rent(remaining));

                            // Only unescape the part we haven't processed.
                            JsonReaderHelper.Unescape(currentPropertyName[idx..], utf8Unescaped, 0, out written);

                            // If the unescaped remainder matches the input remainder, it's a match.
                            if (utf8Unescaped[..written].SequenceEqual(propertyName[idx..]))
                            {
                                // If the property name is a match, the answer is the next element.
                                value = new ResultElement(this, cursor + 1);
                                return true;
                            }
                        }
                        finally
                        {
                            if (rented != null)
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
                value = new ResultElement(this, cursor + 1);
                return true;
            }

            // Move to the previous value
            cursor--;
        }

        value = default;
        return false;
    }

    internal Cursor GetStartCursor(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        (cursor, _) = _metaDb.GetStartCursor(cursor);
        return cursor;
    }

    internal Cursor GetEndCursor(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return cursor + _metaDb.GetNumberOfRows(cursor);
    }
}
