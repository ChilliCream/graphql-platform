using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Text.Json;
using DbRow = HotChocolate.Fusion.Text.Json.CompositeResultDocument.DbRow;
using ElementFlags = HotChocolate.Fusion.Text.Json.CompositeResultDocument.ElementFlags;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// The property name tokens of the objects of one selection set, in the order of their property
/// rows. The table starts with the number of rows such an object spans, followed by one entry
/// per selection: the selection and flags value of the property row as the object template
/// writes it, the length of the name token and the name token itself. A name token is a list
/// separator, the quoted response name and a colon. Its length is zero when the property has no
/// name token, which is the case for an internal selection and for a name that does not fit.
/// </summary>
internal static class PropertyNameTable
{
    /// <summary>
    /// The size of the table header in bytes.
    /// </summary>
    public const int HeaderSize = 8;

    /// <summary>
    /// The size of an entry in bytes.
    /// </summary>
    public const int EntrySize = TokenOffset + MinimizedJsonEmitter.BlockSize;

    /// <summary>
    /// The offset of the name token length within an entry.
    /// </summary>
    public const int LengthOffset = 4;

    /// <summary>
    /// The offset of the name token within an entry.
    /// </summary>
    public const int TokenOffset = 8;

    // A name token is read as one block that starts at the token or one byte after it.
    private const int Padding = 8;

    /// <summary>
    /// Creates the table for <paramref name="selections"/> from the object template rows of
    /// their selection set.
    /// </summary>
    public static byte[] Create(ReadOnlySpan<Selection> selections, ReadOnlySpan<byte> templateRows)
    {
        var table = new byte[HeaderSize + (selections.Length * EntrySize) + Padding];
        var rowCount = (selections.Length * 2) + 1;
        MemoryMarshal.Write(table, in rowCount);

        for (var i = 0; i < selections.Length; i++)
        {
            var entry = table.AsSpan(HeaderSize + (i * EntrySize), EntrySize);
            var propertyRow = templateRows.Slice(((i * 2) + 1) * DbRow.Size, DbRow.Size);
            var selectionAndFlags = MemoryMarshal.Read<int>(propertyRow[DbRow.SelectionAndFlagsOffset..]);
            MemoryMarshal.Write(entry, in selectionAndFlags);

            var name = selections[i].Utf8JsonPropertyName;
            var length = name.Length + 1;

            if (length <= MinimizedJsonEmitter.BlockSize
                && (DbRow.ReadFlags(selectionAndFlags) & (ElementFlags.IsInternal | ElementFlags.IsExcluded))
                    == ElementFlags.None)
            {
                MemoryMarshal.Write(entry[LengthOffset..], in length);
                entry[TokenOffset] = (byte)',';
                name.CopyTo(entry[(TokenOffset + 1)..]);
            }
        }

        return table;
    }

    /// <summary>
    /// Gets the number of rows an object of the table's selection set spans, without its end row.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetRowCount(ReadOnlySpan<byte> table)
        => Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(table));
}
