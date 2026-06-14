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
        private static readonly ArrayPool<MemorySegment> s_arrayPool = ArrayPool<MemorySegment>.Shared;

        private IMemoryArena _arena;
        private MemorySegment[] _chunks;
        private Cursor _cursor;
        private int _rowsInCurrentChunk;
        private bool _disposed;

        // The chunk schedule ramps from Size1K up to Size128K over chunks 0..RampChunks-1; from there
        // every chunk holds MaxRowsPerChunk rows. s_rampRows is the cumulative number of rows the ramp
        // chunks hold, used to size the chunk table from the schedule rather than from the smallest
        // (Size1K) chunk, which would over-rent the table by the ramp's growth factor.
        private const int RampChunks = (int)ChunkSize.Size128K + 1;
        private static readonly int s_maxRowsPerChunk = Cursor.RowsPerChunkFor((int)ChunkSize.Size128K);
        private static readonly int s_rampRows = ComputeRampRows();

        private static int ComputeRampRows()
        {
            var rows = 0;

            for (var i = 0; i < RampChunks; i++)
            {
                rows += Cursor.RowsPerChunkFor(i);
            }

            return rows;
        }

        internal static MetaDb CreateForEstimatedRows(IMemoryArena arena, int estimatedRows)
        {
            // The ramp chunks cover s_rampRows rows across RampChunks chunks; rows beyond the ramp add
            // one constant-size chunk each. +1 guards the boundary so the table never grows mid-parse
            // for a correctly estimated document.
            var chunksNeeded = estimatedRows <= s_rampRows
                ? RampChunks + 1
                : RampChunks + 1 + ((estimatedRows - s_rampRows) / s_maxRowsPerChunk);
            var chunks = s_arrayPool.Rent(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(1, estimatedRows, 1);

            // Clear pooled slots so unallocated chunks are recognizable by a null buffer.
            chunks.AsSpan().Clear();

            // Rent the first chunk now to avoid branching on first append. The document always
            // starts at chunk 0 (Size1K) and ramps up as it grows.
            chunks[0] = arena.Rent(1 << (10 + (int)Cursor.ChunkSizeFor(0)));
            log.ChunkAllocated(1, 0);

            return new MetaDb
            {
                _arena = arena,
                _chunks = chunks,
                _cursor = Cursor.Zero,
                _rowsInCurrentChunk = Cursor.RowsPerChunkFor(0)
            };
        }

        /// <summary>
        /// The cursor that the next <see cref="Append"/> or <see cref="Reserve"/>
        /// call will write to (after handling chunk-boundary advance).
        /// </summary>
        public readonly Cursor NextCursor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var cursor = _cursor;
                if (cursor.Row >= _rowsInCurrentChunk)
                {
                    return Cursor.FromByteOffset(cursor.Chunk + 1, 0);
                }
                return cursor;
            }
        }

        /// <summary>
        /// Allocates the next row slot without writing any data to it. Use
        /// <see cref="Replace"/> later to populate it once all field values
        /// are known. The reserved cursor must not be read from before it is
        /// written.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor Reserve()
        {
            var (cursor, _, _) = AcquireSlot();
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor Append(
            JsonTokenType tokenType,
            int startLocation = DbRow.NoLocation,
            int length = DbRow.UnknownSize,
            int numberOfRows = 1,
            bool hasComplexChildren = false)
        {
            var (cursor, chunk, byteOffset) = AcquireSlot();

            var row = new DbRow(tokenType, startLocation, length, numberOfRows, hasComplexChildren);
            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, chunk.Offset + byteOffset), row);

            return cursor;
        }

        /// <summary>
        /// Overwrites the row at <paramref name="cursor"/> with a freshly
        /// constructed <see cref="DbRow"/>, in a single write.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void Replace(
            Cursor cursor,
            JsonTokenType tokenType,
            int location,
            int sizeOrLength,
            int numberOfRows,
            bool hasComplexChildren = false)
        {
            AssertValidCursor(cursor);

            var row = new DbRow(tokenType, location, sizeOrLength, numberOfRows, hasComplexChildren);
            var chunk = _chunks[cursor.Chunk];
            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, chunk.Offset + cursor.ByteOffset), row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (Cursor cursor, MemorySegment chunk, int byteOffset) AcquireSlot()
        {
            var log = Log;
            var cursor = _cursor;
            var chunks = _chunks.AsSpan();
            var chunksLength = chunks.Length;

            // If current chunk is full, move to next chunk row 0 and refresh the cached row capacity
            // for that chunk (the only place the capacity changes).
            if (cursor.Row >= _rowsInCurrentChunk)
            {
                cursor = Cursor.FromByteOffset(cursor.Chunk + 1, 0);
                _rowsInCurrentChunk = Cursor.RowsPerChunkFor(cursor.Chunk);
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
                    newChunks[i] = default;
                }

                // clear and return old chunks buffer
                chunks.Clear();
                s_arrayPool.Return(_chunks);

                // assign new chunks buffer
                _chunks = newChunks;
                chunks = newChunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];

            // if the chunk has no backing buffer we did not yet rent any memory for it
            if (chunk.Buffer is null)
            {
                var chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));
                chunk = chunks[chunkIndex] = _arena.Rent(chunkBytes);
                log.ChunkAllocated(1, chunkIndex);
            }

            _cursor = Cursor.From(cursor.Chunk, cursor.Row + 1);
            return (cursor, chunk, cursor.ByteOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var chunk = _chunks[cursor.Chunk];
            var dataPos = chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset);

            return MemoryMarshal.Read<DbRow>(dataPos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly JsonTokenType GetJsonTokenType(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // _numberOfRowsAndTypeUnion is the third int in the row.
            var chunk = _chunks[cursor.Chunk];
            var dataPos = chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset + 8);

            var union = MemoryMarshal.Read<uint>(dataPos);
            return (JsonTokenType)(union >> 28);
        }

        [Conditional("DEBUG")]
        private readonly void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(
                cursor.Chunk >= 0 && cursor.Chunk < _chunks.Length,
                $"chunk {cursor.Chunk} out of bounds");
            Debug.Assert(
                _chunks[cursor.Chunk].Buffer is not null,
                $"chunk {cursor.Chunk} is not allocated");

            if (cursor.Chunk < _cursor.Chunk)
            {
                Debug.Assert(
                    cursor.Row >= 0 && cursor.Row < cursor.RowsPerChunk,
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
                Log.MetaDbDisposed(1, chunksLength, cursor.Row);

                // The arena owns the chunk memory and frees it as a whole when it is disposed, so
                // the chunk segments are only dropped here, never returned to a pool. Only the
                // pooled outer array that tracks the segments is returned.
                _chunks.AsSpan().Clear();
                s_arrayPool.Return(_chunks);

                _chunks = [];
                _arena = null!;
                _disposed = true;
            }
        }
    }
}
