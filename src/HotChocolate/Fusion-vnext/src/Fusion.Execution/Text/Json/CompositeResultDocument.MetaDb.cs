using System.Diagnostics;
using System.Runtime.InteropServices;
using static HotChocolate.Text.Json.MetaDbConstants;

namespace HotChocolate.Text.Json;

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
            var chunks = new byte[][chunksNeeded];

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
            int selectionSetId = 0,
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
                selectionSetId,
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
            int selectionSetId = 0,
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
                selectionSetId,
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

        internal ElementTokenType GetElementTokenType(int index)
        {
            // We convert the row index back into a byte offset that we can
            // // in turn break up into the chunk where the row resides and the
            // // local offset we have in that chunk.
            var byteOffset = index * DbRow.Size;
            var chunkIndex = byteOffset / ChunkSize;
            var localOffset = byteOffset % ChunkSize;

            var union = MemoryMarshal.Read<uint>(_chunks[chunkIndex].AsSpan(localOffset + TokenTypeOffset));

            return (ElementTokenType)(union >> 28);
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
