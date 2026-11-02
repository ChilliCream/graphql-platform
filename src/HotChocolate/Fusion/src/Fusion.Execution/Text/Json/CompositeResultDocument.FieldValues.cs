using System.Buffers;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    /// <summary>
    /// Appends the cursors of the non-null values of the field <paramref name="fieldName"/> of the
    /// objects in <paramref name="objects"/>, from <paramref name="start"/> on, to <paramref name="values"/>.
    /// A list value adds the cursors of its non-null elements, with nested lists flattened.
    /// </summary>
    /// <returns>
    /// The index of the first object that has no selection set or whose field holds a leaf value,
    /// or the length of <paramref name="objects"/> if every object was handled.
    /// </returns>
    internal int AppendFieldValues(
        ReadOnlySpan<int> objects,
        int start,
        string fieldName,
        ref PropertyLookupMemo memo,
        ref int[] values,
        ref int count)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        for (var i = start; i < objects.Length; i++)
        {
            var cursor = new Cursor(objects[i]);
            var row = _metaDb.GetValue(ref cursor);

            if (row.TokenType is not ElementTokenType.StartObject
                || row.OperationReferenceType is not OperationReferenceType.SelectionSet)
            {
                return i;
            }

            // Only one row means it was EndObject.
            if (row.NumberOfRows == 1)
            {
                continue;
            }

            var selectionSetId = row.OperationReferenceId;

            if (!memo.Valid || memo.LastSelectionSetId != selectionSetId)
            {
                UpdatePropertyLookupMemo(selectionSetId, fieldName, ref memo);
            }

            if (!memo.Hit)
            {
                continue;
            }

            var valueCursor = cursor + (memo.PropertyRowIndex + 1);
            var valueStart = valueCursor;
            var valueRow = _metaDb.GetValue(ref valueStart);

            switch (valueRow.TokenType)
            {
                case ElementTokenType.None:
                case ElementTokenType.Null:
                    continue;

                case ElementTokenType.StartObject:
                    AppendCursor(ref values, ref count, valueCursor.Value);
                    continue;

                case ElementTokenType.StartArray:
                    AppendListElements(valueStart, valueRow.NumberOfRows, ref values, ref count);
                    continue;

                default:
                    return i;
            }
        }

        return objects.Length;
    }

    private void UpdatePropertyLookupMemo(int selectionSetId, string fieldName, ref PropertyLookupMemo memo)
    {
        var selectionSet = _operation.GetSelectionSetById(selectionSetId);
        memo.LastSelectionSetId = selectionSetId;
        memo.Valid = true;

        if (selectionSet.TryGetSelection(fieldName, out var selection))
        {
            memo.PropertyRowIndex = ((selection.Id - selectionSet.Id - 1) * 2) + 1;
            memo.Hit = true;
            return;
        }

        memo.PropertyRowIndex = 0;
        memo.Hit = false;
    }

    private void AppendListElements(Cursor list, int numberOfRows, ref int[] values, ref int count)
    {
        var end = list + numberOfRows;

        for (var cursor = list + 1; cursor < end; cursor++)
        {
            var element = cursor;
            var row = _metaDb.GetValue(ref element);
            var tokenType = row.TokenType;

            if (tokenType is ElementTokenType.StartArray)
            {
                AppendListElements(element, row.NumberOfRows, ref values, ref count);
            }
            else if (tokenType is ElementTokenType.StartObject
                or (>= ElementTokenType.String and <= ElementTokenType.False))
            {
                AppendCursor(ref values, ref count, cursor.Value);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendCursor(ref int[] values, ref int count, int cursor)
    {
        if (count == values.Length)
        {
            GrowCursors(ref values, count);
        }

        values[count++] = cursor;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GrowCursors(ref int[] values, int count)
    {
        var grown = ArrayPool<int>.Shared.Rent(Math.Max(64, values.Length * 2));
        values.AsSpan(0, count).CopyTo(grown);

        if (values.Length > 0)
        {
            ArrayPool<int>.Shared.Return(values);
        }

        values = grown;
    }
}
