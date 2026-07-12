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
            Debug.Assert((int)flags is >= 0 and <= DbRow.FlagsMask);

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
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));

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
            Debug.Assert((int)flags is >= 0 and <= DbRow.FlagsMask);

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
                    | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));
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
            Debug.Assert((int)flags is >= 0 and <= DbRow.FlagsMask);

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
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));

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
            Debug.Assert((int)flags is >= 0 and <= DbRow.FlagsMask);

            var (chunk, byteOffset, cursor) = ReserveRow();

            ref var dest = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            ref var row = ref Unsafe.Add(ref dest, chunk.Offset + byteOffset);

            // int 0: parent cursor value
            Unsafe.WriteUnaligned(ref row, parentRow);

            // int 1: flags only (no OpRefId / no OpRefType for arrays)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, 4),
                ((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift);

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

        // Overwrites an existing row's value payload in place, preserving only its parent pointer
        // (int 0). Every other packed field is fully rewritten so no stale bits survive from a
        // prior value written into this slot.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void ReplacePreserveParent(
            Cursor cursor,
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int sourceDocumentId = 0,
            int operationReferenceId = 0,
            OperationReferenceType operationReferenceType = OperationReferenceType.None,
            int numberOfRows = 0,
            ElementFlags flags = ElementFlags.None)
        {
            AssertValidCursor(cursor);
            Debug.Assert((int)flags is >= 0 and <= DbRow.FlagsMask);

            ref var row = ref RowRef(cursor);

            // int 1: OperationReferenceId + OperationReferenceType + Flags
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, DbRow.SelectionAndFlagsOffset),
                operationReferenceId
                | ((int)operationReferenceType << 15)
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift));

            // int 2: SizeOrLength (full 32 bits; preserves the sign bit / UnknownSize sentinel)
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref row, DbRow.SizeOffset), sizeOrLength);

            // int 3: Location or NumberOfRows (they share this slot)
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, DbRow.LocationOrRowsOffset),
                (location != 0 ? location : numberOfRows) & 0x1FFFFFFF);

            // int 4: SourceDocumentId + TokenType. int 0 (parent) is intentionally left untouched.
            Unsafe.WriteUnaligned(
                ref Unsafe.Add(ref row, DbRow.SourceAndTypeOffset),
                (sourceDocumentId & 0x7FFF) | (((int)tokenType & 0x0F) << 15));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly DbRow Get(Cursor cursor)
        {
            AssertValidCursor(cursor);

            return Unsafe.ReadUnaligned<DbRow>(ref RowRef(cursor));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly SequentialReader CreateSequentialReader(Cursor cursor)
        {
            AssertValidCursor(cursor);
            return new SequentialReader(_chunks, cursor);
        }

        internal readonly struct PropertyMetadata
        {
            private readonly int _selectionAndFlags;

            internal PropertyMetadata(int selectionAndFlags)
            {
                _selectionAndFlags = selectionAndFlags;
            }

            internal int SelectionId
                => DbRow.ReadOperationReferenceId(_selectionAndFlags);

            internal ElementFlags Flags
                => DbRow.ReadFlags(_selectionAndFlags);
        }

        internal ref struct SequentialReader
        {
            private readonly MemorySegment[] _chunks;
            private ref byte _chunkBase;
            private int _chunkIndex;
            private int _row;
            private int _byteOffset;
            private int _chunkLength;
            private int _segmentOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal SequentialReader(MemorySegment[] chunks, Cursor cursor)
            {
                _chunks = chunks;
                _chunkBase = ref Unsafe.NullRef<byte>();
                _chunkIndex = 0;
                _row = 0;
                _byteOffset = 0;
                _chunkLength = 0;
                _segmentOffset = 0;
                MoveTo(cursor);
            }

            internal readonly Cursor Cursor
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Cursor.From(_chunkIndex, _row);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly DbRow PeekRow()
                => Unsafe.ReadUnaligned<DbRow>(
                    ref Unsafe.Add(ref _chunkBase, _segmentOffset + _byteOffset));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal PropertyMetadata ReadProperty()
            {
                var selectionAndFlags = Unsafe.ReadUnaligned<int>(
                    ref Unsafe.Add(
                        ref _chunkBase,
                        _segmentOffset + _byteOffset + DbRow.SelectionAndFlagsOffset));
                var property = new PropertyMetadata(selectionAndFlags);

                Advance(1);
                return property;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Advance(int rowCount)
            {
                Debug.Assert(rowCount > 0);
                var nextRow = _row + rowCount;

                if ((uint)nextRow < (uint)(_chunkLength / DbRow.Size))
                {
                    _row = nextRow;
                    _byteOffset += rowCount * DbRow.Size;
                    return;
                }

                MoveTo(Cursor.AddRows(rowCount));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void MoveTo(Cursor cursor)
            {
                var segment = _chunks[cursor.Chunk];
                Debug.Assert(segment.Buffer is not null);

                _chunkIndex = cursor.Chunk;
                _row = cursor.Row;
                _byteOffset = cursor.ByteOffset;
                _chunkLength = segment.Length;
                _segmentOffset = segment.Offset;
                _chunkBase = ref MemoryMarshal.GetArrayDataReference(segment.Buffer);
            }
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

            ref var row = ref RowRef(cursor);
            var sourceAndType = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref row, DbRow.SourceAndTypeOffset));
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (tokenType is ElementTokenType.Reference)
            {
                var value = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref row, DbRow.LocationOrRowsOffset)) & 0x1FFFFFFF;
                cursor = new Cursor(value);
                row = ref RowRef(cursor);
                sourceAndType = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref row, DbRow.SourceAndTypeOffset));
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
            }

            return (cursor, tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetLocation(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var locationOrRows = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.LocationOrRowsOffset));
            return locationOrRows & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Cursor GetLocationCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var locationOrRows = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.LocationOrRowsOffset));
            return new Cursor(locationOrRows & 0x1FFFFFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetParent(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var parent = Unsafe.ReadUnaligned<int>(ref RowRef(cursor));
            return parent & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Cursor GetParentCursor(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var parent = Unsafe.ReadUnaligned<int>(ref RowRef(cursor)) & 0x1FFFFFFF;

            return new Cursor(parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetNumberOfRows(Cursor cursor)
        {
            AssertValidCursor(cursor);

            // NumberOfRows shares storage with Location in int 3.
            var value = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.LocationOrRowsOffset));
            return value & 0x1FFFFFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly ElementFlags GetFlags(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var selectionAndFlags = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.SelectionAndFlagsOffset));
            return (ElementFlags)((selectionAndFlags >>> DbRow.FlagsShift) & DbRow.FlagsMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void SetFlags(Cursor cursor, ElementFlags flags)
        {
            AssertValidCursor(cursor);
            Debug.Assert(
                (int)flags is >= 0 and <= DbRow.FlagsMask,
                $"Flags value exceeds {DbRow.FlagsBitCount}-bit limit");
            var fieldSpan = RowSpan(cursor)[DbRow.SelectionAndFlagsOffset..];
            var currentValue = MemoryMarshal.Read<int>(fieldSpan);

            var clearedValue = currentValue & ~(DbRow.FlagsMask << DbRow.FlagsShift);
            var newValue = clearedValue
                | (((int)flags & DbRow.FlagsMask) << DbRow.FlagsShift);

            MemoryMarshal.Write(fieldSpan, newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetSizeOrLength(Cursor cursor)
        {
            AssertValidCursor(cursor);

            var value = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.SizeOffset));

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

            var sourceAndType = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(cursor), DbRow.SourceAndTypeOffset));
            var tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);

            if (resolveReferences && tokenType == ElementTokenType.Reference)
            {
                var value = GetLocation(cursor);
                var resolved = new Cursor(value);
                sourceAndType = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref RowRef(resolved), DbRow.SourceAndTypeOffset));
                tokenType = (ElementTokenType)((sourceAndType >>> 15) & 0x0F);
            }

            return tokenType;
        }

        /// <summary>
        /// Returns a reference to the first byte of the row the cursor points to.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref byte RowRef(Cursor cursor)
        {
            var chunk = _chunks[cursor.Chunk];
            ref var data = ref MemoryMarshal.GetArrayDataReference(chunk.Buffer);
            return ref Unsafe.Add(ref data, chunk.Offset + cursor.ByteOffset);
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
