using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;
using static HotChocolate.Text.Json.MetaDbConstants;

namespace HotChocolate.Text.Json;


public class SourceJsonDocument
{

}

public sealed partial class CompositeResultDocument
{
    private MetaDb _metaDb;
    private byte[][] _dataChunks;
    private List<SourceJsonDocument> _sources;

    public CompositeResultElement RootElement { get; }

    internal struct MetaDb : IDisposable
    {
        private byte[][] _chunks;
        private int _currentChunk;
        private int _currentPosition;
        private bool _disposed;

        internal int Length { get; private set; }

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / RowsPerChunk) + 1);
            var chunks = new byte[][chunksNeeded];

            chunks[0] = MetaDbMemoryPool.Rent();

            for (int i = 1; i < chunks.Length; i++)
            {
                chunks[i] = [];
            }

            return new MetaDb
            {
                _chunks = chunks,
                _currentChunk = 0,
                _currentPosition = 0,
                Length = 0
            };
        }

        internal int Append(
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int selectionSetId = 0,
            ElementFlags flags = ElementFlags.None)
        {
            throw new NotImplementedException();
        }

        internal DbRow Get(int index)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var chunk in _chunks)
                {
                    if (chunk.Length == 0)
                    {
                        break;
                    }

                    MetaDbMemoryPool.Return(chunk);
                }

                _chunks = [];
                _disposed = true;
            }
        }
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
    internal int WriteStartObject(int parentRow = 0, int selectionSetId = 0)
    {
        var flags = ElementFlags.None;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        _metaDb.Append(
            ElementTokenType.StartObject,
            parentRow: parentRow,
            selectionSetId: selectionSetId,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteEndObject() => _metaDb.Append(ElementTokenType.EndObject);

    internal void WriteEmptyProperty(int parentRow, Selection selection)
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

        _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: parentRow,
            selectionSetId: (int)selection.Id,
            flags: flags);

        _metaDb.Append(
            ElementTokenType.None,
            parentRow: parentRow);
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
    public const int ChunkSize = RowsPerChunk * CompositeJsonDocument.DbRow.Size;
    public const int RowsPerChunk = 6552;
}
