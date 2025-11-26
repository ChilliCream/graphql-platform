using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using static HotChocolate.Fusion.Text.Json.MetaDbEventSource;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal struct MetaDb : IDisposable
    {
        private const int SizeOrLengthOffset = 4;
        private const int NumberOfRowsOffset = 8;

        private static readonly ArrayPool<byte[]> s_arrayPool = ArrayPool<byte[]>.Shared;

        private byte[][] _chunks;
        private Cursor _cursor;
        private bool _disposed;

        static MetaDb()
        {
            Debug.Assert(
                JsonMemory.BufferSize >= Cursor.ChunkBytes,
                "MetaDb.BufferSize must match Cursor.ChunkBytes for index math to align.");
        }

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / Cursor.RowsPerChunk) + 1);
            var chunks = s_arrayPool.Rent(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(1, estimatedRows, 1);

            // Rent the first chunk now to avoid branching on first append
            chunks[0] = JsonMemory.Rent(JsonMemoryKind.Metadata);
            log.ChunkAllocated(1, 0);

            for (var i = 1; i < chunks.Length; i++)
            {
                chunks[i] = [];
            }

            return new MetaDb
            {
                _chunks = chunks,
                _cursor = Cursor.Zero
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor Append(
            JsonTokenType tokenType,
            int startLocation = DbRow.NoLocation,
            int length = DbRow.UnknownSize,
            bool hasComplexChildren = false)
        {
            var log = Log;
            var cursor = _cursor;
            var chunks = _chunks.AsSpan();
            var chunksLength = chunks.Length;

            // If current chunk is full, move to next chunk row 0.
            if (cursor.Row >= Cursor.RowsPerChunk)
            {
                cursor = Cursor.FromByteOffset(cursor.Chunk + 1, 0);
            }

            var chunkIndex = cursor.Chunk;

            // make sure we have enough space for the chunk referenced by the chunkIndex.
            if (chunkIndex >= chunksLength)
            {
                // if we do not have enough space we will double the size we have for
                // chunks of memory.
                var nextChunksLength = chunksLength * 2;
                var newChunks = s_arrayPool.Rent(nextChunksLength);
                log.ChunksExpanded(2, chunksLength, nextChunksLength);

                Array.Copy(_chunks, newChunks, chunksLength);

                for (var i = chunksLength; i < nextChunksLength; i++)
                {
                    newChunks[i] = [];
                }

                // clear and return old chunks buffer
                chunks.Clear();
                s_arrayPool.Return(_chunks);

                // assign new chunks buffer
                _chunks = newChunks;
                chunks = newChunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];

            // if the chunk is empty we did not yet rent any memory for it
            if (chunk.Length == 0)
            {
                chunk = chunks[chunkIndex] = JsonMemory.Rent(JsonMemoryKind.Metadata);
                log.ChunkAllocated(1, chunkIndex);
            }

            var byteOffset = cursor.ByteOffset;
            var row = new DbRow(tokenType, startLocation, length, hasComplexChildren);
            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, byteOffset), row);

            _cursor++;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLength(Cursor cursor, int length)
        {
            AssertValidCursor(cursor);
            Debug.Assert(length >= 0);

            var offset = cursor.ByteOffset + SizeOrLengthOffset;
            var destination = _chunks[cursor.Chunk].AsSpan(offset);

            MemoryMarshal.Write(destination, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNumberOfRows(Cursor cursor, int numberOfRows)
        {
            AssertValidCursor(cursor);
            Debug.Assert(numberOfRows is >= 1 and <= 0x0FFFFFFF);

            var offset = cursor.ByteOffset + NumberOfRowsOffset;
            var dataPos = _chunks[cursor.Chunk].AsSpan(offset);
            var current = MemoryMarshal.Read<int>(dataPos);

            var value = (current & unchecked((int)0xF0000000)) | numberOfRows;
            MemoryMarshal.Write(dataPos, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetHasComplexChildren(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var offset = cursor.ByteOffset + SizeOrLengthOffset;
            var dataPos = _chunks[cursor.Chunk].AsSpan(offset);

            var current = MemoryMarshal.Read<int>(dataPos);
            MemoryMarshal.Write(dataPos, current | unchecked((int)0x80000000));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var dataPos = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            return MemoryMarshal.Read<DbRow>(dataPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly JsonTokenType GetJsonTokenType(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var offset = cursor.ByteOffset + NumberOfRowsOffset;
            var dataPos = _chunks[cursor.Chunk].AsSpan(offset);

            var union = MemoryMarshal.Read<uint>(dataPos);
            return (JsonTokenType)(union >> 28);
        }

        [Conditional("DEBUG")]
        private readonly void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(
                cursor.Chunk >= 0 && cursor.Chunk < _chunks.Length,
                $"chunk {cursor.Chunk} out of bounds");

            if (cursor.Chunk < _cursor.Chunk)
            {
                Debug.Assert(
                    cursor.Row is >= 0 and < Cursor.RowsPerChunk,
                    $"row {cursor.Row} out of bounds for chunk {cursor.Chunk}");
            }
            else if (cursor.Chunk == _cursor.Chunk)
            {
                Debug.Assert(
                    cursor.Row >= 0 && cursor.Row < _cursor.Row,
                    $"row {cursor.Row} not yet written in current chunk (max valid {_cursor.Row - 1})");
            }
            else
            {
                Debug.Fail($"chunk {cursor.Chunk} is beyond current chunk {_cursor.Chunk}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var cursor = _cursor;
                var chunksLength = cursor.Chunk + 1;
                var chunks = _chunks.AsSpan(0, chunksLength);
                Log.MetaDbDisposed(1, chunksLength, cursor.Row);

                foreach (var chunk in chunks)
                {
                    if (chunk.Length == 0)
                    {
                        break;
                    }

                    JsonMemory.Return(JsonMemoryKind.Metadata, chunk);
                }

                chunks.Clear();
                s_arrayPool.Return(_chunks);

                _chunks = [];
                _disposed = true;
            }
        }
    }
}
