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
        private static readonly ArrayPool<byte[]> s_arrayPool = ArrayPool<byte[]>.Shared;

        private byte[][] _chunks;
        private Cursor _next;
        private bool _disposed;

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / Cursor.RowsPerChunk) + 1);
            var chunks = s_arrayPool.Rent(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(2, estimatedRows, 1);

            // Rent the first chunk now to avoid branching on first append
            chunks[0] = JsonMemory.Rent(JsonMemoryKind.Metadata);
            log.ChunkAllocated(2, 0);

            for (var i = 1; i < chunks.Length; i++)
            {
                chunks[i] = [];
            }

            return new MetaDb
            {
                _chunks = chunks,
                _next = Cursor.Zero
            };
        }

        public Cursor NextCursor => _next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor Append(
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int parentRow = 0,
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

            if (byteOffset + DbRow.Size > Cursor.ChunkBytes)
            {
                chunkIndex++;
                byteOffset = 0;
                next = Cursor.FromByteOffset(chunkIndex, byteOffset);
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
                log.ChunkAllocated(2, chunkIndex);
            }

            var row = new DbRow(
                tokenType,
                location,
                sizeOrLength,
                parentRow,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, byteOffset), row);

            // Advance write head by one row
            _next = next + 1;
            return next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Replace(
            Cursor cursor,
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int parentRow = 0,
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
                parentRow,
                operationReferenceId,
                operationReferenceType,
                numberOfRows,
                flags);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            MemoryMarshal.Write(span, in row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            return MemoryMarshal.Read<DbRow>(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal (Cursor, ElementTokenType) GetStartCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var chunks = _chunks.AsSpan();
            var span = chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);
            var union = MemoryMarshal.Read<uint>(span[TokenTypeOffset..]);
            var tokenType = (ElementTokenType)(union >> 28);

            if (tokenType is ElementTokenType.Reference)
            {
                var index = MemoryMarshal.Read<int>(span) & 0x07FFFFFF;
                cursor = Cursor.FromIndex(index);
                span = chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset);
                union = MemoryMarshal.Read<uint>(span);
                tokenType = (ElementTokenType)(union >> 28);
            }

            return (cursor, tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetLocation(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var locationAndOpRefType = MemoryMarshal.Read<int>(span);
            return locationAndOpRefType & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetLocationCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var locationAndOpRefType = MemoryMarshal.Read<int>(span);
            return Cursor.FromIndex(locationAndOpRefType & 0x07FFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetParent(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 12);
            return MemoryMarshal.Read<int>(span) & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 12);
            var index = MemoryMarshal.Read<int>(span) & 0x07FFFFFF;
            return Cursor.FromIndex(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset);

            var value = MemoryMarshal.Read<int>(span);
            return value & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 16);

            var value = MemoryMarshal.Read<int>(span);
            return (ElementFlags)((value >> 15) & 0x1FF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert((short)flags <= 511, "Flags value exceeds 9-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 16);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = currentValue & unchecked((int)0xFF007FFF); // ~(0x1FF << 15)
            var newValue = clearedValue | ((int)flags << 15);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 4);
            var value = MemoryMarshal.Read<int>(span);

            return value & 0x07FFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetSizeOrLength(Cursor cursor, int sizeOrLength)
        {
            AssertValidCursor(cursor);
            Debug.Assert(sizeOrLength >= 0 && sizeOrLength <= 0x07FFFFFF, "SizeOrLength value exceeds 27-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 4);
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

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset);
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

            var union = MemoryMarshal.Read<uint>(_chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset));
            var tokenType = (ElementTokenType)(union >> 28);

            if (resolveReferences && tokenType == ElementTokenType.Reference)
            {
                var idx = GetLocation(cursor);
                var resolved = Cursor.FromIndex(idx);
                union = MemoryMarshal.Read<uint>(_chunks[resolved.Chunk].AsSpan(resolved.ByteOffset + TokenTypeOffset));
                tokenType = (ElementTokenType)(union >> 28);
            }

            return tokenType;
        }

        [Conditional("DEBUG")]
        private void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(cursor.Chunk >= 0, "Negative chunk");
            Debug.Assert(cursor.Chunk < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[cursor.Chunk].Length > 0, "Accessing unallocated chunk");

            var maxExclusive = _next.Chunk * Cursor.RowsPerChunk + _next.Row;
            var absoluteIndex = (cursor.Chunk * Cursor.RowsPerChunk) + cursor.Row;

            Debug.Assert(absoluteIndex >= 0 && absoluteIndex < maxExclusive,
                $"Cursor points to row {absoluteIndex}, but only {maxExclusive} rows are valid.");
            Debug.Assert(cursor.ByteOffset + DbRow.Size <= JsonMemory.BufferSize, "Cursor byte offset out of bounds");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var cursor = _next;
                var chunksLength = cursor.Chunk + 1;
                var chunks = _chunks.AsSpan(0, chunksLength);
                Log.MetaDbDisposed(2, chunksLength, cursor.Row);

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
