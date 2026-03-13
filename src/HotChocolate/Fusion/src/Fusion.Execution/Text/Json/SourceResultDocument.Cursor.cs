using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    /// <summary>
    /// Comparable MetaDb cursor (chunk, row).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal readonly struct Cursor : IEquatable<Cursor>, IComparable<Cursor>
    {
        public const int DbRowSize = 12;
        public const int ChunkBytes = 1 << 17;
        public const int RowsPerChunk = ChunkBytes / DbRowSize;
        public const int MaxChunks = 4096;

        private const int RowBits = 14;
        private const int ChunkBits = 12;
        private const int ChunkShift = RowBits;

        private const uint RowMask = (1u << RowBits) - 1u;
        private const uint ChunkMask = (1u << ChunkBits) - 1u;

        private readonly uint _value;

        static Cursor()
        {
            Debug.Assert(DbRowSize > 0, "Row size must be > 0");
            Debug.Assert(RowsPerChunk > 0, "RowsPerChunk must be > 0");
            Debug.Assert(RowsPerChunk <= (int)(RowMask + 1), "RowBits too small for RowsPerChunk");
            Debug.Assert(MaxChunks <= (int)(ChunkMask + 1), "ChunkBits too small for MaxChunks");
        }

        public static readonly Cursor Zero = From(0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Cursor(uint value) => _value = value;

        /// <summary>
        /// Create from validated (chunk,row) parts.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert((uint)rowWithinChunk < RowsPerChunk);
            return new Cursor(((uint)chunkIndex << ChunkShift) | (uint)rowWithinChunk);
        }

        /// <summary>
        /// Try create without asserts; false if out of range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFrom(int chunkIndex, int rowWithinChunk, out Cursor idx)
        {
            if ((uint)chunkIndex >= MaxChunks || (uint)rowWithinChunk >= RowsPerChunk)
            {
                idx = default;
                return false;
            }

            idx = From(chunkIndex, rowWithinChunk);
            return true;
        }

        /// <summary>
        /// Create from a byte offset inside the chunk (must be row-aligned).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromByteOffset(int chunkIndex, int byteOffset)
        {
            Debug.Assert(byteOffset % DbRowSize == 0);
            return From(chunkIndex, byteOffset / DbRowSize);
        }

        public uint Value => _value;

        public int Chunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_value >> ChunkShift) & ChunkMask);
        }

        /// <summary>
        /// Row index within the chunk (0..RowsPerChunk-1).
        /// </summary>
        public int Row
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_value & RowMask);
        }

        /// <summary>
        /// Byte offset within the chunk (row * DbRowSize).
        /// </summary>
        public int ByteOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Row * DbRowSize;
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
                // positive carry
                var carry = row / RowsPerChunk;
                row -= carry * RowsPerChunk;
                chunk += carry;
            }
            else if (row < 0)
            {
                // borrow across chunks (floor-div toward -∞)
                var borrow = (-row + RowsPerChunk - 1) / RowsPerChunk;
                row += borrow * RowsPerChunk;
                chunk -= borrow;
            }

            // Clamp (policy: clamp + debug)
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
        public bool Equals(Cursor other) => _value == other._value;

        public override bool Equals(object? obj) => obj is Cursor p && Equals(p);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator uint(Cursor p) => p._value;

        public void Deconstruct(out int chunk, out int row)
        {
            chunk = Chunk;
            row = Row;
        }
    }
}
