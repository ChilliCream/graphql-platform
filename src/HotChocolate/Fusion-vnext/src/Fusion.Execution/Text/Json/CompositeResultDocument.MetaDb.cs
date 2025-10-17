using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal struct MetaDb : IDisposable
    {
        private const int TokenTypeOffset = 8;

        private byte[][] _chunks;
        private Cursor _next;
        private bool _disposed;

        internal static MetaDb CreateForEstimatedRows(int estimatedRows)
        {
            var chunksNeeded = Math.Max(4, (estimatedRows / Cursor.RowsPerChunk) + 1);
            var chunks = ArrayPool<byte[]>.Shared.Rent(chunksNeeded);

            // Rent the first chunk now to avoid branching on first append
            chunks[0] = MetaDbMemory.Rent();

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
            int sourceDocumentId = 0,
            int parentRow = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
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
                var newChunks = ArrayPool<byte[]>.Shared.Rent(nextChunksLength);

                Array.Copy(_chunks, newChunks, chunksLength);

                for (var i = chunksLength; i < nextChunksLength; i++)
                {
                    newChunks[i] = [];
                }

                _chunks = newChunks;
                chunks = newChunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];

            // if the chunk is empty we did not yet rent any memory for it
            if (chunk.Length == 0)
            {
                chunk = chunks[chunkIndex] = MetaDbMemory.Rent();
            }

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
            int sourceDocumentId = 0,
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
                sourceDocumentId,
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
                return (cursor, tokenType);
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

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var sourceAndParentHigh = MemoryMarshal.Read<int>(span[12..]);
            var selectionSetFlagsAndParentLow = MemoryMarshal.Read<int>(span[16..]);

            return (sourceAndParentHigh >>> 15 << 11)
                | ((selectionSetFlagsAndParentLow >> 21) & 0x7FF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var sourceAndParentHigh = MemoryMarshal.Read<int>(span[12..]);
            var selectionSetFlagsAndParentLow = MemoryMarshal.Read<int>(span[16..]);

            var index = (sourceAndParentHigh >>> 15 << 11)
                | ((selectionSetFlagsAndParentLow >> 21) & 0x7FF);

            return Cursor.FromIndex(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset);

            var value = MemoryMarshal.Read<int>(span);
            return value & 0x0FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 16);

            var selectionSetFlagsAndParentLow = MemoryMarshal.Read<int>(span);
            return (ElementFlags)((selectionSetFlagsAndParentLow >> 15) & 0x3F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert((byte)flags <= 63, "Flags value exceeds 6-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 16);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = currentValue & 0xFFE07FFF; // ~(0x3F << 15)
            var newValue = (int)(clearedValue | (uint)((int)flags << 15));

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 4);
            var value = MemoryMarshal.Read<int>(span);

            return value & int.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetSizeOrLength(Cursor cursor, int sizeOrLength)
        {
            AssertValidCursor(cursor);
            Debug.Assert(sizeOrLength >= 0 && sizeOrLength <= int.MaxValue, "SizeOrLength value exceeds 31-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + 4);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Keep only the sign bit (HasComplexChildren)
            var clearedValue = currentValue & unchecked((int)0x80000000);
            var newValue = clearedValue | (sizeOrLength & int.MaxValue);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNumberOfRows(Cursor cursor, int numberOfRows)
        {
            AssertValidCursor(cursor);
            Debug.Assert(numberOfRows >= 0 && numberOfRows <= 0x0FFFFFFF, "NumberOfRows value exceeds 28-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + TokenTypeOffset);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Keep only the top 4 bits (token type)
            var clearedValue = currentValue & unchecked((int)0xF0000000);
            var newValue = clearedValue | (numberOfRows & 0x0FFFFFFF);

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
            var absoluteIndex = cursor.Chunk * Cursor.RowsPerChunk + cursor.Row;

            Debug.Assert(absoluteIndex >= 0 && absoluteIndex < maxExclusive,
                $"Cursor points to row {absoluteIndex}, but only {maxExclusive} rows are valid.");
            Debug.Assert(cursor.ByteOffset + DbRow.Size <= MetaDbMemory.BufferSize, "Cursor byte offset out of bounds");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var cursor = _next;
                var chunks = _chunks.AsSpan(0, cursor.Chunk + 1);

                foreach (var chunk in chunks)
                {
                    if (chunk.Length == 0)
                    {
                        break;
                    }

                    MetaDbMemory.Return(chunk);
                }

                chunks.Clear();
                ArrayPool<byte[]>.Shared.Return(_chunks);

                _chunks = [];
                _disposed = true;
            }
        }
    }
}
