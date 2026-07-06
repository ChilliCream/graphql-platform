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
        private IMemoryArena _arena;
        private MemorySegment[] _chunks;
        private Cursor _next;
        // Byte capacity of the chunk _next currently points into. The geometric schedule derives it
        // from the chunk index, so it only changes when _next moves to a new chunk; appends compare
        // against this cached value instead of recomputing the shift each time.
        private int _chunkBytes;
        private bool _disposed;

        internal static MetaDb Create(IMemoryArena arena)
        {
            const int chunksNeeded = 4;
            var chunks = arena.RentSegmentTable(chunksNeeded);
            var log = Log;

            log.MetaDbCreated(2, Cursor.RowsPerChunkFor(0), 1);

            // Clear pooled slots so unallocated chunks are recognizable by a null buffer.
            chunks.AsSpan().Clear();

            // Rent the first chunk now to avoid branching on first append. The document always
            // starts at chunk 0 (Size1K) and ramps up as it grows.
            var chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(0));
            chunks[0] = arena.Rent(chunkBytes);
            log.ChunkAllocated(2, 0);

            return new MetaDb
            {
                _arena = arena,
                _chunks = chunks,
                _next = Cursor.CreateZero(),
                _chunkBytes = chunkBytes
            };
        }

        public readonly Cursor NextCursor => _next;

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

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref dest, chunk.Offset + byteOffset), row);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendNull(int parentRow)
        {
            Debug.Assert(parentRow is >= 0 and <= 0x1FFFFFFF);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

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
            Debug.Assert((byte)flags <= 127);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: selectionId + opRefType=Selection + flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                selectionId
                | ((int)OperationReferenceType.Selection << 15)
                | ((int)flags << 17));

            // ints 2..3 must be zero (int 4 is written directly below)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 8), 0, 8);

            // int 4: PropertyName token
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
            Debug.Assert((byte)flags <= 127);

            var next = _next;
            var byteOffset = next.ByteOffset;
            var chunks = _chunks;

            // Fast path: both rows fit in the current chunk.
            if (byteOffset + (DbRow.Size * 2) <= _chunkBytes
                && (uint)next.Chunk < (uint)chunks.Length
                && chunks[next.Chunk] is { Buffer: { } buffer } segment)
            {
                ref var dest = ref MemoryMarshal.GetArrayDataReference(buffer);
                ref var row0 = ref Unsafe.Add(ref dest, segment.Offset + byteOffset);

                // Row 0: PropertyName
                Unsafe.WriteUnaligned(ref row0, parentRow);
                Unsafe.WriteUnaligned(
                    ref Unsafe.Add(ref row0, 4),
                    selectionId
                    | ((int)OperationReferenceType.Selection << 15)
                    | ((int)flags << 17));
                Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row0, 8), 0, 8);
                // int 4: PropertyName token
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
            Debug.Assert((byte)flags <= 127);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

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
            Debug.Assert((byte)flags <= 127);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

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

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

            // int 0: no parent
            Unsafe.WriteUnaligned(ref row, 0);
            // ints 1..3 zeroed (int 4 is written directly below)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 12);
            // int 4: EndObject token
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, 16), (int)ElementTokenType.EndObject << 15);

            _next = cursor + 1;
            return cursor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Cursor AppendEndArray()
        {
            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

            // int 0: no parent
            Unsafe.WriteUnaligned(ref row, 0);
            // ints 1..3 zeroed (int 4 is written directly below)
            Unsafe.InitBlockUnaligned(ref Unsafe.Add(ref row, 4), 0, 12);
            // int 4: EndArray token
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
            var bytesNeeded = count * DbRow.Size;

            // Fast path: all rows fit in the current chunk.
            if (byteOffset + bytesNeeded <= _chunkBytes
                && next.Chunk < _chunks.Length
                && _chunks[next.Chunk] is { Buffer: { } buffer } segment)
            {
                ref var dest = ref MemoryMarshal.GetArrayDataReference(buffer);
                ref var region = ref Unsafe.Add(ref dest, segment.Offset + byteOffset);

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
        private (MemorySegment chunk, int byteOffset, Cursor cursor) ReserveRow()
        {
            var next = _next;
            var chunkIndex = next.Chunk;
            var byteOffset = next.ByteOffset;

            if (byteOffset + DbRow.Size > _chunkBytes)
            {
                chunkIndex++;
                byteOffset = 0;
                next = Cursor.FromByteOffset(chunkIndex, byteOffset);
                // The chunk size follows the geometric schedule, so it is derived from the index.
                _chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));
            }

            var chunks = _chunks.AsSpan();
            if (chunkIndex >= chunks.Length)
            {
                GrowChunks(chunks.Length);
                chunks = _chunks.AsSpan();
            }

            var chunk = chunks[chunkIndex];
            if (chunk.Buffer is null)
            {
                // A new chunk is materialized here both on the byte-overflow roll above and after a
                // fast path filled the previous chunk exactly, so refresh the cached capacity.
                _chunkBytes = 1 << (10 + (int)Cursor.ChunkSizeFor(chunkIndex));
                chunk = chunks[chunkIndex] = _arena.Rent(_chunkBytes);
                Log.ChunkAllocated(2, chunkIndex);
            }

            return (chunk, byteOffset, next);
        }

        private void GrowChunks(int currentLength)
        {
            _arena.GrowSegmentTable(ref _chunks);
            Log.ChunksExpanded(2, currentLength, _chunks.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void Replace(
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

            MemoryMarshal.Write(RowSpan(cursor), in row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            return MemoryMarshal.Read<DbRow>(RowSpan(cursor));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow GetValue(ref Cursor cursor)
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
        internal readonly (Cursor, ElementTokenType) GetStartCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var span = RowSpan(cursor);
            var sourceAndType = MemoryMarshal.Read<int>(span[DbRow.SourceAndTypeOffset..]);
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (tokenType is ElementTokenType.Reference)
            {
                var value = MemoryMarshal.Read<int>(span[DbRow.LocationOrRowsOffset..]) & 0x1FFFFFFF;
                cursor = new Cursor(value);
                span = RowSpan(cursor);
                sourceAndType = MemoryMarshal.Read<int>(span[DbRow.SourceAndTypeOffset..]);
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
            }

            return (cursor, tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetLocation(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var locationOrRows = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.LocationOrRowsOffset..]);
            return locationOrRows & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Cursor GetLocationCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var locationOrRows = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.LocationOrRowsOffset..]);
            return new Cursor(locationOrRows & 0x1FFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetParent(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var parent = MemoryMarshal.Read<int>(RowSpan(cursor));
            return parent & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var parent = MemoryMarshal.Read<int>(RowSpan(cursor)) & 0x1FFFFFFF;

            return new Cursor(parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // NumberOfRows shares storage with Location in int 3.
            var value = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.LocationOrRowsOffset..]);
            return value & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var selectionAndFlags = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.SelectionAndFlagsOffset..]);
            return (ElementFlags)((selectionAndFlags >> 17) & 0x7F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert((byte)flags <= 127, "Flags value exceeds 7-bit limit");

            var fieldSpan = RowSpan(cursor)[DbRow.SelectionAndFlagsOffset..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Clear bits 17..23 (7-bit Flags region) then OR new flags in.
            var clearedValue = (int)((uint)currentValue & ~(0x7Fu << 17));
            var newValue = clearedValue | ((int)flags << 17);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var value = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.SizeOffset..]);

            return value & int.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void SetSizeOrLength(Cursor cursor, int sizeOrLength)
        {
            AssertValidCursor(cursor);
            Debug.Assert(sizeOrLength >= 0, "SizeOrLength value exceeds 31-bit limit");

            var fieldSpan = RowSpan(cursor)[DbRow.SizeOffset..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            // Keep only the sign bit (HasComplexChildren)
            var clearedValue = currentValue & unchecked((int)0x80000000);
            var newValue = clearedValue | (sizeOrLength & int.MaxValue);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void SetNumberOfRows(Cursor cursor, int numberOfRows)
        {
            AssertValidCursor(cursor);
            Debug.Assert(numberOfRows >= 0 && numberOfRows <= 0x1FFFFFFF, "NumberOfRows value exceeds 29-bit limit");

            // NumberOfRows shares storage with Location in int 3. Preserve the 3 reserved
            // high bits and write the 29-bit value into the low bits.
            var fieldSpan = RowSpan(cursor)[DbRow.LocationOrRowsOffset..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = (int)((uint)currentValue & 0xE0000000u);
            var newValue = clearedValue | (numberOfRows & 0x1FFFFFFF);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ElementTokenType GetElementTokenType(Cursor cursor, bool resolveReferences = true)
        {
            AssertValidCursor(cursor);

            var sourceAndType = MemoryMarshal.Read<int>(RowSpan(cursor)[DbRow.SourceAndTypeOffset..]);
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (resolveReferences && tokenType == ElementTokenType.Reference)
            {
                var value = GetLocation(cursor);
                var resolved = new Cursor(value);
                sourceAndType = MemoryMarshal.Read<int>(RowSpan(resolved)[DbRow.SourceAndTypeOffset..]);
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
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

        [Conditional("DEBUG")]
        private readonly void AssertValidCursor(Cursor cursor)
        {
            Debug.Assert(cursor.Chunk >= 0, "Negative chunk");
            Debug.Assert(cursor.Chunk < _chunks.Length, "Chunk index out of bounds");
            Debug.Assert(_chunks[cursor.Chunk].Buffer is not null, "Accessing unallocated chunk");

            var maxExclusive = _next.Index;
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

                _chunks = [];
                _arena = null!;
                _disposed = true;
            }
        }
    }
}
