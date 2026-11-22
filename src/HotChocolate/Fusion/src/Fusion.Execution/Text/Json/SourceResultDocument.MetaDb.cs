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
        private IMemoryArena _arena;
        private MemorySegment[] _chunks;
        private Cursor _cursor;
        private int _rowsInCurrentChunk;
        private byte[] _currentBuffer;
        private int _currentOffset;
        private bool _disposed;

        // The chunk schedule ramps from Size1K up to Size128K over chunks 0..RampChunks-1; from there
        // every chunk holds MaxRowsPerChunk rows. s_rampRows is the cumulative number of rows the ramp
        // chunks hold, used to size the chunk table from the schedule rather than from the smallest
        // (Size1K) chunk, which would over-rent the table by the ramp's growth factor.
        private const int RampChunks = ChunkSchedule.RampLength + 1;
        private const int MinTableChunks = 8;
        private static readonly int s_maxRowsPerChunk = Cursor.RowsPerChunkFor(ChunkSchedule.RampLength);
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

        // The number of ramp chunks that hold the given number of rows, never below MinTableChunks.
        private static int CountRampChunks(int rows)
        {
            var chunks = 0;

            while (rows > 0 && chunks < RampChunks)
            {
                rows -= Cursor.RowsPerChunkFor(chunks++);
            }

            return Math.Max(chunks, MinTableChunks);
        }

        internal static MetaDb CreateForEstimatedRows(IMemoryArena arena, int estimatedRows)
        {
            // The ramp chunks cover s_rampRows rows across RampChunks chunks; rows beyond the ramp add
            // one constant-size chunk each. +1 guards the boundary so the table never grows mid-parse
            // for a correctly estimated document.
            var chunksNeeded = estimatedRows <= s_rampRows
                ? CountRampChunks(estimatedRows) + 1
                : RampChunks + 1 + ((estimatedRows - s_rampRows) / s_maxRowsPerChunk);
            var chunks = arena.RentSegmentTable(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(1, estimatedRows, 1);

            // Rent the first chunk now to avoid branching on first append. The document always
            // starts at chunk 0 (Size1K) and ramps up as it grows.
            var first = chunks[0] = arena.Rent(1 << (10 + (int)Cursor.ChunkSizeFor(0)));
            log.ChunkAllocated(1, 0);

            return new MetaDb
            {
                _arena = arena,
                _chunks = chunks,
                _cursor = Cursor.Zero,
                _rowsInCurrentChunk = Cursor.RowsPerChunkFor(0),
                _currentBuffer = first.Buffer,
                _currentOffset = first.Offset
            };
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

        /// <summary>
        /// Makes the chunk at <paramref name="chunkIndex"/> the current chunk, with the cursor at
        /// its first row.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StartChunk(int chunkIndex)
        {
            _rowsInCurrentChunk = Cursor.RowsPerChunkFor(chunkIndex);

            // make sure we have enough space for the chunk referenced by the chunkIndex.
            if (chunkIndex >= _chunks.Length)
            {
                // if we do not have enough space we will double the size we have for
                // chunks of memory.
                GrowChunks(_chunks.Length);
            }

            var chunk = _chunks[chunkIndex];

            // if the chunk has no backing buffer we did not yet rent any memory for it
            if (chunk.Buffer is null)
            {
                var chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));
                chunk = _chunks[chunkIndex] = _arena.Rent(chunkBytes);
                Log.ChunkAllocated(1, chunkIndex);
            }

            _currentBuffer = chunk.Buffer;
            _currentOffset = chunk.Offset;
            _cursor = Cursor.From(chunkIndex, 0);
        }

        private void GrowChunks(int currentLength)
        {
            _arena.GrowSegmentTable(ref _chunks);
            Log.ChunksExpanded(2, currentLength, _chunks.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            ref readonly var chunk = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(_chunks),
                cursor.Chunk);

            return Unsafe.ReadUnaligned<DbRow>(
                ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                    chunk.Offset + cursor.ByteOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly JsonTokenType GetJsonTokenType(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // _numberOfRowsAndTypeUnion is the third int in the row.
            ref readonly var chunk = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(_chunks),
                cursor.Chunk);

            var union = Unsafe.ReadUnaligned<uint>(
                ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                    chunk.Offset + cursor.ByteOffset + 8));

            return (JsonTokenType)(union >> 28);
        }

        /// <summary>
        /// Gets the position directly after the last written row.
        /// </summary>
        internal readonly Cursor EndCursor => _cursor;

        /// <summary>
        /// Gets the read-only rows written so far. The builder must not be written to afterwards.
        /// </summary>
        internal readonly ReadOnlyMetaDb Freeze()
#if DEBUG
            => new(_chunks, _cursor);
#else
            => new(_chunks);
#endif

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

                // The arena owns the chunk memory and the chunk table, and frees both as a whole
                // when it is disposed, so neither is returned here.
                _chunks = [];
                _currentBuffer = null!;
                _arena = null!;
                _disposed = true;
            }
        }

        /// <summary>
        /// Appends rows to a <see cref="MetaDb"/> from a position that it keeps itself. Between
        /// the creation of the writer and <see cref="Complete"/> the end of the database is not
        /// defined.
        /// </summary>
        internal ref struct RowWriter
        {
            // The slot of the next row, the number of rows that the current chunk still takes and
            // the cursor of the next row. The cursor of a full chunk is the row after its last row.
            private ref byte _slot;
            private int _freeRows;
            private uint _cursor;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RowWriter(scoped ref MetaDb metaDb)
            {
                var cursor = metaDb._cursor;

                _slot = ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(metaDb._currentBuffer),
                    metaDb._currentOffset + cursor.ByteOffset);
                _freeRows = metaDb._rowsInCurrentChunk - cursor.Row;
                _cursor = cursor.Value;
            }

            /// <summary>
            /// The cursor that the next <see cref="Append"/> or <see cref="Reserve"/> call writes to.
            /// </summary>
            public readonly Cursor NextCursor
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _freeRows != 0
                    ? Cursor.FromValue(_cursor)
                    : Cursor.From(Cursor.FromValue(_cursor).Chunk + 1, 0);
            }

            /// <summary>
            /// Takes the next row without writing it and returns its cursor. The row must be
            /// written with <see cref="MetaDb.Replace"/> before it is read.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Cursor Reserve(scoped ref MetaDb metaDb)
            {
                if (_freeRows == 0)
                {
                    MoveToNextChunk(ref metaDb);
                }

                var cursor = Cursor.FromValue(_cursor);

                _slot = ref Unsafe.Add(ref _slot, DbRow.Size);
                _freeRows--;
                _cursor++;
                TrackEnd(ref metaDb);
                return cursor;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Append(
                scoped ref MetaDb metaDb,
                JsonTokenType tokenType,
                int startLocation,
                int length,
                int numberOfRows,
                bool hasComplexChildren)
            {
                if (_freeRows == 0)
                {
                    MoveToNextChunk(ref metaDb);
                }

                Unsafe.WriteUnaligned(
                    ref _slot,
                    new DbRow(tokenType, startLocation, length, numberOfRows, hasComplexChildren));
                _slot = ref Unsafe.Add(ref _slot, DbRow.Size);
                _freeRows--;
                _cursor++;
                TrackEnd(ref metaDb);
            }

            /// <summary>
            /// Hands the position of the writer back to <paramref name="metaDb"/>, which then ends
            /// after the last row of the writer.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly void Complete(scoped ref MetaDb metaDb)
                => metaDb._cursor = Cursor.FromValue(_cursor);

            // The assertions of the database compare cursors with its end.
            [Conditional("DEBUG")]
            private readonly void TrackEnd(scoped ref MetaDb metaDb)
                => metaDb._cursor = Cursor.FromValue(_cursor);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void MoveToNextChunk(scoped ref MetaDb metaDb)
            {
                var chunkIndex = Cursor.FromValue(_cursor).Chunk + 1;

                metaDb.StartChunk(chunkIndex);
                _slot = ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(metaDb._currentBuffer),
                    metaDb._currentOffset);
                _freeRows = Cursor.RowsPerChunkFor(chunkIndex);
                _cursor = Cursor.From(chunkIndex, 0).Value;
            }
        }
    }

    /// <summary>
    /// The read-only rows of a parsed document.
    /// </summary>
    internal readonly struct ReadOnlyMetaDb
    {
        private readonly MemorySegment[] _chunks;
#if DEBUG
        private readonly Cursor _end;

        public ReadOnlyMetaDb(MemorySegment[] chunks, Cursor end)
        {
            _chunks = chunks;
            _end = end;
        }
#else
        public ReadOnlyMetaDb(MemorySegment[] chunks)
        {
            _chunks = chunks;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            ref readonly var chunk = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(_chunks),
                cursor.Chunk);

            return Unsafe.ReadUnaligned<DbRow>(
                ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                    chunk.Offset + cursor.ByteOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokenType GetJsonTokenType(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // _numberOfRowsAndTypeUnion is the third int in the row.
            ref readonly var chunk = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(_chunks),
                cursor.Chunk);

            var union = Unsafe.ReadUnaligned<uint>(
                ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(chunk.Buffer),
                    chunk.Offset + cursor.ByteOffset + 8));

            return (JsonTokenType)(union >> 28);
        }

        [Conditional("DEBUG")]
        private void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(
                cursor.Chunk >= 0 && cursor.Chunk < _chunks.Length,
                $"chunk {cursor.Chunk} out of bounds");
            Debug.Assert(
                _chunks[cursor.Chunk].Buffer is not null,
                $"chunk {cursor.Chunk} is not allocated");

#if DEBUG
            if (cursor.Chunk < _end.Chunk)
            {
                Debug.Assert(
                    cursor.Row >= 0 && cursor.Row < cursor.RowsPerChunk,
                    $"row {cursor.Row} out of bounds for chunk {cursor.Chunk}");
            }
            else if (cursor.Chunk == _end.Chunk)
            {
                Debug.Assert(
                    cursor.Row >= 0 && cursor.Row < _end.Row,
                    $"row {cursor.Row} not yet written in current chunk (max valid {_end.Row - 1})");
            }
            else
            {
                Debug.Fail($"chunk {cursor.Chunk} is beyond current chunk {_end.Chunk}");
            }
#endif
        }
    }
}
