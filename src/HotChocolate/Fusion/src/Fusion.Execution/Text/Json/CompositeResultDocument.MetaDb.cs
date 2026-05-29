using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using HotChocolate.Buffers;
using static HotChocolate.Fusion.Text.Json.MetaDbEventSource;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal struct MetaDb : IDisposable
    {
        private static readonly ArrayPool<byte[]> s_arrayPool = ArrayPool<byte[]>.Shared;

        private byte[][] _chunks;
        private Cursor _next;
        private bool _disposed;

        internal static MetaDb Create(Cursor zero)
        {
            const int chunksNeeded = 4;
            var chunks = s_arrayPool.Rent(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(2, zero.RowsPerChunk, 1);

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
                _next = zero
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
            var (chunk, byteOffset, cursor) = ReserveRow();

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

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendNull(int parentRow)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: parent cursor value (TokenType=None lives at off 16 and is zero here)
            Unsafe.WriteUnaligned(ref row, parentRow);
            // ints 1..4 stamped zero via a single 16-byte vector store
            // (off 16 = TokenType.None(0) for free)
            Vector128<byte>.Zero.StoreUnsafe(ref Unsafe.Add(ref row, 4));

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendEmptyProperty(int parentRow, int selectionId, ElementFlags flags)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);
            Debug.Assert(selectionId is >= 0 and <= 0x7FFF);
            Debug.Assert((byte)flags <= 63);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: selectionId + opRefType=Selection + flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                selectionId
                | ((int)OperationReferenceType.Selection << 15)
                | ((int)flags << 17));

            // ints 2..4 must be zero (this clears off 16 too)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 8), 0, 12);

            // int 4: PropertyName token (written AFTER the zero-fill that covers off 16)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 16),
                (int)ElementTokenType.PropertyName << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendEmptyPropertyWithNullValue(int parentRow, int selectionId, ElementFlags flags)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);
            Debug.Assert(selectionId is >= 0 and <= 0x7FFF);
            Debug.Assert((byte)flags <= 63);

            var next = _next;
            var byteOffset = next.ByteOffset;
            var chunkBytes = 1 << (10 + (int)next.ChunkSize);
            var chunks = _chunks;

            // Fast path: both rows fit in the current chunk.
            if (byteOffset + (DbRow.Size * 2) <= chunkBytes
                && (uint)next.Chunk < (uint)chunks.Length
                && chunks[next.Chunk] is { Length: > 0 } chunk)
            {
                ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
                ref var row0 = ref Unsafe.Add(ref dest, byteOffset);

                // Row 0: PropertyName
                Unsafe.WriteUnaligned(ref row0, parentRow);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref row0, 4),
                    selectionId
                    | ((int)OperationReferenceType.Selection << 15)
                    | ((int)flags << 17));
                Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row0, 8), 0, 12);
                // PropertyName token at off 16, after the zero-fill that covers it.
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref row0, 16),
                    (int)ElementTokenType.PropertyName << 15);

                // Row 1: None value with parent = cursor value of Row 0 (= next.Value)
                ref var row1 = ref Unsafe.Add(ref row0, DbRow.Size);
                Unsafe.WriteUnaligned(ref row1, next.Value);
                Vector128<byte>.Zero.StoreUnsafe(ref Unsafe.Add(ref row1, 4));

                _next = next + 2;
                return next;
            }

            // Slow path: crosses chunk boundary or chunk not yet rented
            var propCursor = AppendEmptyProperty(parentRow, selectionId, flags);
            AppendNull(propCursor.Value);
            return propCursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendStartObject(int parentRow, int selectionSetId, int propertyCount, ElementFlags flags)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);
            Debug.Assert(selectionSetId is >= 0 and <= 0x7FFF);
            Debug.Assert(propertyCount is >= 0 and <= 0x0FFFFFFF); // room for (count*2)+1 in 29 bits
            Debug.Assert((byte)flags <= 63);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: selectionSetId + SelectionSet + flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                selectionSetId
                | ((int)OperationReferenceType.SelectionSet << 15)
                | ((int)flags << 17));

            // int 2: sizeOrLength = property count
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), propertyCount);

            // int 3: numberOfRows = (count * 2) + 1   (1 property = 2 rows: name + value)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 12),
                ((propertyCount * 2) + 1) & 0x1FFFFFFF);

            // int 4: StartObject token (sourceDocumentId = 0)
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), (int)ElementTokenType.StartObject << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendStartArray(int parentRow, int length, ElementFlags flags)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);
            Debug.Assert(length is >= 0 and <= int.MaxValue);
            Debug.Assert((byte)flags <= 63);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: flags only (no OpRefId / no OpRefType for arrays)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                (int)flags << 17);

            // int 2: sizeOrLength = length
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 8), length);

            // int 3: numberOfRows = length + 1
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 12), (length + 1) & 0x1FFFFFFF);

            // int 4: StartArray token (sourceDocumentId = 0)
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), (int)ElementTokenType.StartArray << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendEndObject()
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: no parent
            Unsafe.WriteUnaligned(ref row, 0);
            // ints 1..4 zeroed (this clears off 16 too)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 16);
            // int 4: EndObject token, after the zero-fill that covers off 16
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), (int)ElementTokenType.EndObject << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendEndArray()
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
            ref var row = ref Unsafe.Add(ref dest, byteOffset);

            // int 0: no parent
            Unsafe.WriteUnaligned(ref row, 0);
            // ints 1..4 zeroed (this clears off 16 too)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 16);
            // int 4: EndArray token, after the zero-fill that covers off 16
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), (int)ElementTokenType.EndArray << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AppendNullRange(int parentRow, int count)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);
            Debug.Assert(count >= 0);

            if (count == 0)
            {
                return;
            }

            var next = _next;
            var byteOffset = next.ByteOffset;
            var chunkBytes = 1 << (10 + (int)next.ChunkSize);
            var bytesNeeded = count * DbRow.Size;

            // Fast path: all rows fit in the current chunk.
            if (byteOffset + bytesNeeded <= chunkBytes
                && next.Chunk < _chunks.Length
                && _chunks[next.Chunk].Length > 0)
            {
                var chunk = _chunks[next.Chunk];

                ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk);
                ref var region = ref Unsafe.Add(ref dest, byteOffset);

                // Zero the whole range once (off 16 = TokenType.None for free), then
                // stamp the parent cursor value into int 0 of each row.
                Unsafe.InitBlockUnaligned(ref region, 0, (uint)bytesNeeded);

                for (var i = 0; i < count; i++)
                {
                    Unsafe.WriteUnaligned(
                        ref Unsafe.Add(ref region, i * DbRow.Size),
                        parentRow);
                }

                _next = next + count;
                return;
            }

            // Slow path: crosses chunk boundary or chunk not yet rented.
            for (var i = 0; i < count; i++)
            {
                AppendNull(parentRow);
            }
        }

        /// <summary>
        /// Reserves the next row slot, advancing to a new chunk if necessary. Does not
        /// advance <see cref="_next"/>; the caller updates it after writing the row.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (byte[] chunk, int byteOffset, Cursor cursor) ReserveRow()
        {
            var next = _next;
            var chunkIndex = next.Chunk;
            var byteOffset = next.ByteOffset;
            var chunkBytes = 1 << (10 + (int)next.ChunkSize);

            if (byteOffset + DbRow.Size > chunkBytes)
            {
                chunkIndex++;
                byteOffset = 0;
                next = next.FromByteOffset(chunkIndex, byteOffset);
            }

            var chunks = _chunks.AsSpan();
            if (chunkIndex >= chunks.Length)
            {
                GrowChunks(chunks.Length);
                chunks = _chunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];
            if (chunk.Length == 0)
            {
                chunk = chunks[chunkIndex] = JsonMemory.Rent(JsonMemoryKind.Metadata);
                Log.ChunkAllocated(2, chunkIndex);
            }

            return (chunk, byteOffset, next);
        }

        private void GrowChunks(int currentLength)
        {
            var nextLength = currentLength * 2;
            var newChunks = s_arrayPool.Rent(nextLength);
            Log.ChunksExpanded(2, currentLength, nextLength);

            Array.Copy(_chunks, newChunks, currentLength);

            for (var i = currentLength; i < newChunks.Length; i++)
            {
                newChunks[i] = [];
            }

            _chunks.AsSpan().Clear();
            s_arrayPool.Return(_chunks);
            _chunks = newChunks;
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
        internal DbRow GetValue(ref Cursor cursor)
        {
            var row = Get(cursor);

            if (row.TokenType is ElementTokenType.Reference)
            {
                cursor = new Cursor(row.Location);
                row = Get(cursor);
            }

            return row;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal (Cursor, ElementTokenType) GetStartCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var chunks = _chunks.AsSpan();
            var span = chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);
            var sourceAndType = MemoryMarshal.Read<int>(span[DbRow.SourceAndTypeOffset..]);
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (tokenType is ElementTokenType.Reference)
            {
                var value = MemoryMarshal.Read<int>(span[DbRow.LocationOrRowsOffset..]) & 0x1FFFFFFF;
                cursor = new Cursor(value);
                span = chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);
                sourceAndType = MemoryMarshal.Read<int>(span[DbRow.SourceAndTypeOffset..]);
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
            }

            return (cursor, tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetLocation(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.LocationOrRowsOffset);

            var locationOrRows = MemoryMarshal.Read<int>(span);
            return locationOrRows & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetLocationCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.LocationOrRowsOffset);

            var locationOrRows = MemoryMarshal.Read<int>(span);
            return new Cursor(locationOrRows & 0x1FFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetParent(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var parent = MemoryMarshal.Read<int>(span);
            return parent & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset);

            var parent = MemoryMarshal.Read<int>(span) & 0x1FFFFFFF;

            return new Cursor(parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // NumberOfRows shares storage with Location in int 3.
            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.LocationOrRowsOffset);

            var value = MemoryMarshal.Read<int>(span);
            return value & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.SelectionAndFlagsOffset);

            var selectionAndFlags = MemoryMarshal.Read<int>(span);
            return (ElementFlags)((selectionAndFlags >> 17) & 0x3F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert((byte)flags <= 63, "Flags value exceeds 6-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.SelectionAndFlagsOffset);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Clear bits 17..22 (6-bit Flags region) then OR new flags in.
            var clearedValue = (int)((uint)currentValue & ~(0x3Fu << 17));
            var newValue = clearedValue | ((int)flags << 17);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.SizeOffset);
            var value = MemoryMarshal.Read<int>(span);

            return value & int.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetSizeOrLength(Cursor cursor, int sizeOrLength)
        {
            AssertValidCursor(cursor);
            Debug.Assert(sizeOrLength >= 0, "SizeOrLength value exceeds 31-bit limit");

            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.SizeOffset);
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
            Debug.Assert(numberOfRows >= 0 && numberOfRows <= 0x1FFFFFFF, "NumberOfRows value exceeds 29-bit limit");

            // NumberOfRows shares storage with Location in int 3. Preserve the 3 reserved
            // high bits and write the 29-bit value into the low bits.
            var fieldSpan = _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.LocationOrRowsOffset);
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = (int)((uint)currentValue & 0xE0000000u);
            var newValue = clearedValue | (numberOfRows & 0x1FFFFFFF);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ElementTokenType GetElementTokenType(Cursor cursor, bool resolveReferences = true)
        {
            AssertValidCursor(cursor);

            var sourceAndType = MemoryMarshal.Read<int>(
                _chunks[cursor.Chunk].AsSpan(cursor.ByteOffset + DbRow.SourceAndTypeOffset));
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (resolveReferences && tokenType == ElementTokenType.Reference)
            {
                var value = GetLocation(cursor);
                var resolved = new Cursor(value);
                sourceAndType = MemoryMarshal.Read<int>(
                    _chunks[resolved.Chunk].AsSpan(resolved.ByteOffset + DbRow.SourceAndTypeOffset));
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
            }

            return tokenType;
        }

        [Conditional("DEBUG")]
        private void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(cursor.Chunk >= 0, "Negative chunk");
            Debug.Assert(cursor.Chunk < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[cursor.Chunk].Length > 0, "Accessing unallocated chunk");

            var maxExclusive = _next.Index;
            var absoluteIndex = cursor.Index;

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
