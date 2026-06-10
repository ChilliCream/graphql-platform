using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Buffers;
using static HotChocolate.Text.Json.MetaDbEventSource;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    internal struct MetaDb : IDisposable
    {
        private const int TokenTypeOffset = 8;
        private static readonly ArrayPool<MemorySegment> s_arrayPool = ArrayPool<MemorySegment>.Shared;

        private IMemoryArena _arena;
        private MemorySegment[] _chunks;
        private MemorySegment[]? _previousChunks;
        private Cursor _next;
        private volatile int _nextValue;
        private bool _disposed;

        internal static MetaDb Create(IMemoryArena arena)
        {
            var chunks = s_arrayPool.Rent(4);
            var log = Log;

            log.MetaDbCreated(2, 0, 1);

            // Clear pooled slots so unallocated chunks are recognizable by a null buffer.
            chunks.AsSpan().Clear();

            // Rent the first chunk now to avoid branching on first append. The document always
            // starts at chunk 0 (Size1K) and ramps up as it grows.
            chunks[0] = arena.Rent(1 << (10 + (int)Cursor.ChunkSizeFor(0)));
            log.ChunkAllocated(2, 0);

            var zero = Cursor.CreateZero();

            return new MetaDb
            {
                _arena = arena,
                _chunks = chunks,
                _next = zero,
                _nextValue = zero.Value
            };
        }

        public readonly Cursor NextCursor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var value = _nextValue;
                return Unsafe.As<int, Cursor>(ref value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor Append(
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int parent = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            var log = Log;
            var next = _next;
            var chunkIndex = next.Chunk;
            var byteOffset = next.ByteOffset;

            var chunks = _chunks.AsSpan();
            var chunksLength = chunks.Length;

            // The chunk size follows the geometric schedule, so it is derived from the chunk index.
            var chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));

            if (byteOffset + DbRow.Size > chunkBytes)
            {
                chunkIndex++;
                byteOffset = 0;
                next = Cursor.FromByteOffset(chunkIndex, byteOffset);
                chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));
            }

            // make sure we have enough space for the chunk referenced by the chunkIndex.
            if (chunkIndex >= chunksLength)
            {
                // if we do not have enough space we will double the size we have for
                // chunks of memory.
                var nextChunksLength = chunksLength * 2;
                var newChunks = s_arrayPool.Rent(nextChunksLength);
                log.ChunksExpanded(2, chunksLength, nextChunksLength);

                // copy chunks to new buffer
                Array.Copy(_chunks, newChunks, chunksLength);

                for (var i = chunksLength; i < nextChunksLength; i++)
                {
                    newChunks[i] = default;
                }

                // Concurrent readers may still reference the current chunks array.
                // Return the previously retained one and keep the current one
                // alive until the next expansion or Dispose.
                if (_previousChunks is not null)
                {
                    _previousChunks.AsSpan().Clear();
                    s_arrayPool.Return(_previousChunks);
                }

                _previousChunks = _chunks;

                // assign new chunks buffer
                _chunks = newChunks;
                chunks = newChunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];

            // if the chunk has no backing buffer we did not yet rent any memory for it
            if (chunk.Buffer is null)
            {
                chunk = chunks[chunkIndex] = _arena.Rent(chunkBytes);
                log.ChunkAllocated(2, chunkIndex);
            }

            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                parent,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, chunk.Offset + byteOffset), row);

            // Advance write head by one row
            var newNext = next + 1;
            _next = newNext;
            _nextValue = newNext.Value;
            return next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Replace(
            Cursor cursor,
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int parent = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            AssertValidCursor(cursor);

            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                parent,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            var span = RowSpan(cursor);

            MemoryMarshal.Write(span, in row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor);

            return MemoryMarshal.Read<DbRow>(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal (Cursor, ElementTokenType) GetStartCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var chunks = _chunks.AsSpan();
            var span = RowSpan(chunks, cursor);
            var union = MemoryMarshal.Read<uint>(span[TokenTypeOffset..]);
            var tokenType = (ElementTokenType)(union >> 28);

            if (tokenType is ElementTokenType.Reference)
            {
                var value = MemoryMarshal.Read<int>(span) & 0x1FFFFFFF;
                cursor = new Cursor(value);
                span = RowSpan(chunks, cursor)[TokenTypeOffset..];
                union = MemoryMarshal.Read<uint>(span);
                tokenType = (ElementTokenType)(union >> 28);
            }

            return (cursor, tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetLocation(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor);

            var location = MemoryMarshal.Read<int>(span);
            return location & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetLocationCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor);

            var location = MemoryMarshal.Read<int>(span);
            return new Cursor(location & 0x1FFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetParent(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor)[12..];
            return MemoryMarshal.Read<int>(span) & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor)[12..];
            var value = MemoryMarshal.Read<int>(span) & 0x1FFFFFFF;
            return new Cursor(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor)[TokenTypeOffset..];

            var value = MemoryMarshal.Read<int>(span);
            return value & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor)[16..];

            var value = MemoryMarshal.Read<int>(span);
            return (ElementFlags)((value >> 15) & 0x1FF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert((short)flags <= 511, "Flags value exceeds 9-bit limit");

            var fieldSpan = RowSpan(cursor)[16..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = currentValue & unchecked((int)0xFF007FFF); // ~(0x1FF << 15)
            var newValue = clearedValue | ((int)flags << 15);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor)[4..];
            var value = MemoryMarshal.Read<int>(span);

            return value & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetSizeOrLength(Cursor cursor, int sizeOrLength)
        {
            AssertValidCursor(cursor);
            Debug.Assert(sizeOrLength >= 0 && sizeOrLength <= 0x07FFFFFF, "SizeOrLength value exceeds 27-bit limit");

            var fieldSpan = RowSpan(cursor)[4..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Keep only the sign bit (HasComplexChildren) + 4 reserved bits
            var clearedValue = currentValue & unchecked((int)0xF8000000);
            var newValue = clearedValue | (sizeOrLength & 0x07FFFFFF);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNumberOfRows(Cursor cursor, int numberOfRows)
        {
            AssertValidCursor(cursor);
            Debug.Assert(numberOfRows >= 0 && numberOfRows <= 0x07FFFFFF, "NumberOfRows value exceeds 27-bit limit");

            var fieldSpan = RowSpan(cursor)[TokenTypeOffset..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Keep only the top 5 bits (4 bits token type + 1 reserved)
            var clearedValue = currentValue & unchecked((int)0xF8000000);
            var newValue = clearedValue | (numberOfRows & 0x07FFFFFF);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementTokenType GetElementTokenType(Cursor cursor, bool resolveReferences = true)
        {
            AssertValidCursor(cursor);

            var union = MemoryMarshal.Read<uint>(RowSpan(cursor)[TokenTypeOffset..]);
            var tokenType = (ElementTokenType)(union >> 28);

            if (resolveReferences && tokenType == ElementTokenType.Reference)
            {
                var value = GetLocation(cursor);
                var resolved = new Cursor(value);
                union = MemoryMarshal.Read<uint>(RowSpan(resolved)[TokenTypeOffset..]);
                tokenType = (ElementTokenType)(union >> 28);
            }

            return tokenType;
        }

        /// <summary>
        /// Returns the span that begins at the start of the row the cursor points to.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly Span<byte> RowSpan(Cursor cursor)
        {
            var chunk = _chunks[cursor.Chunk];
            return chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset, DbRow.Size);
        }

        /// <summary>
        /// Returns the span that begins at the start of the row the cursor points to,
        /// reading the chunk from the supplied snapshot of the chunk array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> RowSpan(Span<MemorySegment> chunks, Cursor cursor)
        {
            var chunk = chunks[cursor.Chunk];
            return chunk.Buffer.AsSpan(chunk.Offset + cursor.ByteOffset, DbRow.Size);
        }

        [Conditional("DEBUG")]
        private readonly void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(cursor.Chunk >= 0, "Negative chunk");
            Debug.Assert(cursor.Chunk < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[cursor.Chunk].Buffer is not null, "Accessing unallocated chunk");

            var value = _nextValue;
            var maxCursor = Unsafe.As<int, Cursor>(ref value);
            var maxExclusive = maxCursor.Index;
            var absoluteIndex = cursor.Index;

            Debug.Assert(absoluteIndex >= 0 && absoluteIndex < maxExclusive,
                $"Cursor points to row {absoluteIndex}, but only {maxExclusive} rows are valid.");

            var chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(cursor.Chunk));
            Debug.Assert(cursor.ByteOffset + DbRow.Size <= chunkBytes, "Cursor byte offset out of bounds");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var cursor = _next;
                var chunksLength = cursor.Chunk + 1;
                Log.MetaDbDisposed(2, chunksLength, cursor.Row);

                // The arena owns the chunk memory and frees it as a whole when it is disposed,
                // so the chunk segments are only dropped here, never returned to a pool. Only the
                // pooled outer arrays that track the segments are returned.
                if (_previousChunks is not null)
                {
                    _previousChunks.AsSpan().Clear();
                    s_arrayPool.Return(_previousChunks);
                    _previousChunks = null;
                }

                _chunks.AsSpan().Clear();
                s_arrayPool.Return(_chunks);

                _chunks = [];
                _arena = null!;
                _disposed = true;
            }
        }
    }
}
