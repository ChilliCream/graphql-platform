using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;
using static HotChocolate.Fusion.Text.Json.MetaDbConstants;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultDocument
{
    internal int Id;

}

public struct SourceResultElement
{
    internal SourceResultDocument Parent;
    internal int Index;
    internal int Size;
    internal ElementTokenType TokenType;
    internal JsonValueKind ValueKind;
    internal bool HasComplexChildren;
}

public sealed partial class CompositeResultDocument
{
    private static Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _metaDb;
    private byte[][] _dataChunks;
    private List<SourceResultDocument> _sources;
    private Operation _operation;
    private bool _disposed;

    public CompositeResultElement RootElement { get; }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ElementTokenType GetElementTokenType(int index)
        => _metaDb.GetElementTokenType(index);

    internal CompositeResultElement GetArrayIndexElement(int currentIndex, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        var arrayLength = row.NumberOfRows;

        if ((uint)arrayIndex >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        return new CompositeResultElement(this, currentIndex + arrayIndex + 1);
    }

    internal int GetArrayLength(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        return row.NumberOfRows;
    }

    internal int GetPropertyCount(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartObject, row.TokenType);

        return row.NumberOfRows;
    }

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
    {
        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.SelectionSetId).RawResponseName;
        }

        if (row.TokenType == ElementTokenType.Reference)
        {
        }

        throw new NotImplementedException();
    }

    private CompositeResultElement CreateObject(int parentRow, SelectionSet selectionSet)
    {
        // change to int
        var index = WriteStartObject(parentRow, (int)selectionSet.Id);

        foreach (var selection in selectionSet.Selections)
        {
            WriteEmptyProperty(index, selection);
        }

        WriteEndObject();

        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteLeaveValue(CompositeResultElement target, SourceResultElement source)
    {
        _metaDb.Replace(
            index: target.MetadataDbIndex,
            tokenType: source.TokenType,
            location: source.Index,
            sizeOrLength: source.Size,
            sourceDocumentId: source.Parent.Id,
            parentRow: _metaDb.Get(target.MetadataDbIndex).ParentRow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteStartObject(int parentRow = 0, int selectionSetId = 0)
    {
        var flags = ElementFlags.None;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.Append(
            ElementTokenType.StartObject,
            parentRow: parentRow,
            selectionSetId: selectionSetId,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndObject() => _metaDb.Append(ElementTokenType.EndObject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(int parentRow, Selection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (selection.IsLeaf)
        {
            flags |= ElementFlags.IsLeaf;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        var index = _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: parentRow,
            selectionSetId: (int)selection.Id,
            flags: flags);

        _metaDb.Append(
            ElementTokenType.None,
            parentRow: index);
    }

    private static void CheckExpectedType(ElementTokenType expected, ElementTokenType actual)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(expected, actual);
    }
}

internal static class MetaDbMemoryPool
{
    public static byte[] Rent() => new byte[ChunkSize];

    public static void Return(byte[] chunk)
    {
    }
}

internal static class MetaDbConstants
{
    // 6552 rows × 20 bytes
    public const int ChunkSize = RowsPerChunk * CompositeResultDocument.DbRow.Size;
    public const int RowsPerChunk = 6552;
}
