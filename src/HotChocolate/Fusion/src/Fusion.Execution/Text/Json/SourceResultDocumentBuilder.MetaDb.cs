using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

internal sealed partial class SourceResultDocumentBuilder
{
    internal struct MetaDb : IDisposable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PooledArrayWriter _buffer = new();
        private int _length;

        public MetaDb()
        {
        }

        public DbRow Get(int index)
        {
            var span = _buffer.WrittenSpan;
            var offset = index * DbRow.Size;
            return MemoryMarshal.Read<DbRow>(span.Slice(offset, DbRow.Size));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ElementTokenType GetElementTokenType(int index)
        {
            var span = _buffer.WrittenSpan;
            // _numberOfRowsAndTypeUnion is at offset 8
            var offset = (index * DbRow.Size) + 8;

            var value = MemoryMarshal.Read<uint>(span[offset..]);
            return (ElementTokenType)(value >> 28); // Top 4 bits (logical shift)
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetNumberOfRows(int index)
        {
            var span = _buffer.WrittenSpan;
            // _numberOfRowsAndTypeUnion is at offset 8
            var offset = (index * DbRow.Size) + 8;

            var value = MemoryMarshal.Read<uint>(span[offset..]);
            return (int)(value & 0x0FFFFFFFu); // Lower 28 bits
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetLocation(int index)
        {
            var span = _buffer.WrittenSpan;
            // _location is at offset 0
            var offset = index * DbRow.Size;

            return MemoryMarshal.Read<int>(span[offset..]);
        }

        public int Append(
            ElementTokenType tokenType,
            int location = 0,
            int sizeOrLength = 0,
            int rows = 1,
            bool hasComplexChildren = false)
        {
            var index = _length;
            var row = new DbRow(
                tokenType: tokenType,
                location: location,
                sizeOrLength: sizeOrLength,
                rows: rows,
                hasComplexChildren: hasComplexChildren);

            var span = _buffer.GetSpan(DbRow.Size);
            MemoryMarshal.Write(span, in row);
            _buffer.Advance(DbRow.Size);
            _length++;

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetSizeOrLength(int index, int sizeOrLength)
        {
            var span = _buffer.Memory.Span;
            // Skip _location field
            var offset = (index * DbRow.Size) + 4;

            // Read current value to preserve HasComplexChildren flag (sign bit)
            var current = MemoryMarshal.Read<int>(span[offset..]);
            var hasComplexChildren = current < 0;

            // Write new value with preserved flag
            var updated = hasComplexChildren
                ? sizeOrLength | int.MinValue
                : sizeOrLength & int.MaxValue;
            MemoryMarshal.Write(span[offset..], in updated);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetRows(int index, int rows)
        {
            var span = _buffer.Memory.Span;
            // Skip _location and _sizeOrLengthUnion fields
            var offset = (index * DbRow.Size) + 8;

            // Read current value to preserve TokenType (top 4 bits)
            var current = MemoryMarshal.Read<uint>(span[offset..]);
            var tokenType = current & 0xF0000000u;

            // Write new value with preserved token type
            var updated = tokenType | ((uint)rows & 0x0FFFFFFFu);
            MemoryMarshal.Write(span[offset..], in updated);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetLocation(int index, int location)
        {
            var span = _buffer.Memory.Span;
            // _location is the first field
            var offset = index * DbRow.Size;

            MemoryMarshal.Write(span[offset..], in location);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetElementTokenType(int index, ElementTokenType tokenType)
        {
            var span = _buffer.Memory.Span;
            // Skip _location and _sizeOrLengthUnion fields
            var offset = (index * DbRow.Size) + 8;

            // Read current value to preserve rows (lower 28 bits)
            var current = MemoryMarshal.Read<uint>(span[offset..]);
            var rows = current & 0x0FFFFFFFu;

            // Write new value with preserved rows
            var updated = ((uint)tokenType << 28) | rows;
            MemoryMarshal.Write(span[offset..], in updated);
        }

        public void Dispose() => _buffer.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DbRow
    {
        internal const int Size = 12;
        internal const int UnknownSize = -1;

        private readonly int _location;
        private readonly int _sizeOrLengthUnion;
        private readonly int _numberOfRowsAndTypeUnion;

        internal DbRow(ElementTokenType tokenType, int location, int sizeOrLength, int rows, bool hasComplexChildren)
        {
            Debug.Assert(tokenType is >= ElementTokenType.None and <= ElementTokenType.Reference);
            Debug.Assert((byte)tokenType < 1 << 4);
            Debug.Assert(location >= 0);
            Debug.Assert(sizeOrLength >= UnknownSize);
            Debug.Assert(Unsafe.SizeOf<DbRow>() == Size);

            _location = location;
            _sizeOrLengthUnion = hasComplexChildren
                // Set sign bit
                ? sizeOrLength | int.MinValue
                // Clear sign bit
                : sizeOrLength & int.MaxValue;
            _numberOfRowsAndTypeUnion = ((int)tokenType << 28) | rows;
        }

        internal int Location => _location;

        internal int SizeOrLength => _sizeOrLengthUnion & int.MaxValue;

        internal bool IsUnknownSize => _sizeOrLengthUnion == UnknownSize;

        internal bool HasComplexChildren => _sizeOrLengthUnion < 0;

        internal int NumberOfRows => _numberOfRowsAndTypeUnion & 0x0FFFFFFF;

        internal ElementTokenType TokenType => (ElementTokenType)(unchecked((uint)_numberOfRowsAndTypeUnion) >> 28);

        internal bool IsSimpleValue => TokenType >= ElementTokenType.PropertyName;
    }
}
