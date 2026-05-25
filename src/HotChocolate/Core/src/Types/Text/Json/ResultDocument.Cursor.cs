using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    /// <summary>
    /// Comparable MetaDb cursor (chunk, row)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal readonly struct Cursor : IEquatable<Cursor>, IComparable<Cursor>
    {
        public const int ChunkBytes = 1 << 17;
        public const int RowsPerChunk = ChunkBytes / DbRow.Size;
        public const int MaxChunks = 4096;

        private const int RowBits = 14;
        private const int ChunkBits = 12;
        private const int ChunkShift = RowBits;

        private const uint RowMask = (1u << RowBits) - 1u;
        private const uint ChunkMask = (1u << ChunkBits) - 1u;

        private readonly uint _value;

        public static readonly Cursor Zero = From(0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Cursor(uint value) => _value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert((uint)rowWithinChunk < RowsPerChunk);
            return new Cursor(((uint)chunkIndex << ChunkShift) | (uint)rowWithinChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromIndex(int rowIndex)
        {
            Debug.Assert(rowIndex >= 0);
            var chunk = rowIndex / RowsPerChunk;
            var row = rowIndex - (chunk * RowsPerChunk);
            return From(chunk, row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromByteOffset(int chunkIndex, int byteOffset)
        {
            Debug.Assert(byteOffset % DbRow.Size == 0, "byteOffset must be row-aligned.");
            return From(chunkIndex, byteOffset / DbRow.Size);
        }

        public uint Value => _value;

        public int Chunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_value >> ChunkShift) & ChunkMask);
        }

        public int Row
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_value & RowMask);
        }

        public int ByteOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Row * DbRow.Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor AddRows(int delta)
        {
            if (delta == 0)
            {
                return this;
            }

            var row = Row + delta;
            var chunk = Chunk;

            if (row >= RowsPerChunk)
            {
                var carry = row / RowsPerChunk;
                row -= carry * RowsPerChunk;
                chunk += carry;
            }
            else if (row < 0)
            {
                var borrow = (-row + RowsPerChunk - 1) / RowsPerChunk;
                row += borrow * RowsPerChunk;
                chunk -= borrow;
            }

            if (chunk < 0)
            {
                Debug.Fail("Cursor underflow");
                chunk = 0;
                row = 0;
            }
            else if (chunk >= MaxChunks)
            {
                Debug.Fail("Cursor overflow");
                chunk = MaxChunks - 1;
                row = RowsPerChunk - 1;
            }

            return From(chunk, row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithChunk(int chunk) => From(chunk, Row);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithRow(int row) => From(Chunk, row);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToIndex() => (Chunk * RowsPerChunk) + Row;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToTotalBytes() => (Chunk * ChunkBytes) + ByteOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Cursor other) => _value == other._value;

        public override bool Equals(object? obj) => obj is Cursor c && Equals(c);

        public override int GetHashCode() => (int)_value;

        public override string ToString() => $"chunk={Chunk}, row={Row} (0x{_value:X8})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Cursor other) => _value.CompareTo(other._value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Cursor a, Cursor b) => a._value == b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Cursor a, Cursor b) => a._value != b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Cursor a, Cursor b) => a._value < b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Cursor a, Cursor b) => a._value > b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Cursor a, Cursor b) => a._value <= b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Cursor a, Cursor b) => a._value >= b._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor operator +(Cursor x, int delta) => x.AddRows(delta);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor operator -(Cursor x, int delta) => x.AddRows(-delta);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor operator ++(Cursor x) => x.AddRows(1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor operator --(Cursor x) => x.AddRows(-1);
    }
}
