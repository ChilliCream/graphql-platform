using System.Diagnostics;
using System.Runtime.InteropServices;
using static HotChocolate.Fusion.Text.Json.MetaDbConstants;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal struct MetaDb : IDisposable
    {
        const int TokenTypeOffset = 8;

        private byte[][] _chunks;
        private int _currentChunk;
        private int _currentPosition;
        private bool _disposed;

        internal int Length { get; private set; }

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / RowsPerChunk) + 1);
            var chunks = new byte[chunksNeeded][];

            chunks[0] = MetaDbMemoryPool.Rent();

            for (var i = 1; i < chunks.Length; i++)
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
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            // Check if we need to allocate a new chunk
            if (_currentPosition + DbRow.Size > ChunkSize)
            {
                _currentChunk++;
                _currentPosition = 0;

                // Ensure we have space in the chunks array
                if (_currentChunk >= _chunks.Length)
                {
                    // todo: we might want to pool this
                    // If we need to grow the chunks array we will do
                    // so by doubling the space.
                    var newChunks = new byte[_chunks.Length * 2][];
                    Array.Copy(_chunks, newChunks, _chunks.Length);

                    // Each new space will be initialized with am empty array
                    for (var i = _chunks.Length; i < newChunks.Length; i++)
                    {
                        newChunks[i] = [];
                    }

                    _chunks = newChunks;
                }

                // If the current selected chunk is empty then we
                // just have filled up a block and must rent more memory.
                if (_chunks[_currentChunk].Length == 0)
                {
                    _chunks[_currentChunk] = MetaDbMemoryPool.Rent();
                }
            }

            // Calculate the global row index for return value
            var rowIndex = Length / DbRow.Size;

            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                sourceDocumentId,
                parentRow,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            // Write the row to the current chunk
            MemoryMarshal.Write(_chunks[_currentChunk].AsSpan(_currentPosition), in row);

            // Update position and length for the next append
            _currentPosition += DbRow.Size;
            Length += DbRow.Size;

            return rowIndex;
        }

        internal void Replace(
            int index,
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < Length / DbRow.Size, "Index out of bounds");

            // We convert the row index back into a byte offset that we can
            // in turn break up into the chunk where the row resides and the
            // local offset we have in that chunk.
            var offset = index * DbRow.Size;
            var chunkIndex = offset / ChunkSize;
            var localOffset = offset % ChunkSize;

            Debug.Assert(chunkIndex < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[chunkIndex].Length > 0, "Accessing unallocated chunk");

            // We create a new row to replace the current data.
            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                sourceDocumentId,
                parentRow,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            // Then write it all back to the chunk.
            MemoryMarshal.Write(_chunks[chunkIndex].AsSpan(localOffset), in row);
        }

        internal DbRow Get(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < Length / DbRow.Size, "Index out of bounds");

            // We convert the row index back into a byte offset that we can
            // // in turn break up into the chunk where the row resides and the
            // // local offset we have in that chunk.
            var byteOffset = index * DbRow.Size;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            Debug.Assert(chunkIndex < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[chunkIndex].Length > 0, "Accessing unallocated chunk");

            return MemoryMarshal.Read<DbRow>(_chunks[chunkIndex].AsSpan(localOffset));
        }

        internal int GetLocation(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < Length / DbRow.Size, "Index out of bounds");

            // Convert row index to byte offset
            var byteOffset = index * DbRow.Size;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            Debug.Assert(chunkIndex < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[chunkIndex].Length > 0, "Accessing unallocated chunk");

            // Read the first field that contains the Location bits
            var locationAndOpRefType = MemoryMarshal.Read<int>(_chunks[chunkIndex].AsSpan(localOffset));

            // Extract Location from the low 27 bits
            return locationAndOpRefType & 0x07FFFFFF;
        }

        internal int GetParentRow(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < Length / DbRow.Size, "Index out of bounds");

            // Convert row index to byte offset
            var byteOffset = index * DbRow.Size;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            Debug.Assert(chunkIndex < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[chunkIndex].Length > 0, "Accessing unallocated chunk");

            // Read the two fields that contain the ParentRow bits
            var sourceAndParentHigh = MemoryMarshal.Read<int>(_chunks[chunkIndex].AsSpan(localOffset + 12)); // Offset to 4th field
            var selectionSetFlagsAndParentLow = MemoryMarshal.Read<int>(_chunks[chunkIndex].AsSpan(localOffset + 16)); // Offset to 5th field

            // Reconstruct ParentRow from high and low bits (same logic as DbRow property)
            return ((int)((uint)sourceAndParentHigh >> 15) << 11) | ((selectionSetFlagsAndParentLow >> 21) & 0x7FF);
        }

        internal ElementTokenType GetElementTokenType(int index)
        {
            // We convert the row index back into a byte offset that we can
            // in turn break up into the chunk where the row resides and the
            // local offset we have in that chunk.
            var byteOffset = index * DbRow.Size;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            var union = MemoryMarshal.Read<uint>(_chunks[chunkIndex].AsSpan(localOffset + TokenTypeOffset));
            var tokenType = (ElementTokenType)(union >> 28);

            if (tokenType is ElementTokenType.Reference)
            {
                index = GetLocation(index);
                byteOffset = index * DbRow.Size;
                chunkIndex = byteOffset / ChunkSize;
                localOffset = byteOffset % ChunkSize;
                union = MemoryMarshal.Read<uint>(_chunks[chunkIndex].AsSpan(localOffset + TokenTypeOffset));
                tokenType = (ElementTokenType)(union >> 28);
            }

            return tokenType;
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
}
