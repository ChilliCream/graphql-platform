using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument
{
    /// <summary>
    /// Comparable MetaDb cursor
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal readonly struct Cursor : IEquatable<Cursor>, IComparable<Cursor>
    {
        public const int MaxChunks = 4096;

        private const int RowBits = 14;
        private const int ChunkBits = 12;
        private const int SizeBits = 3;
        private const int ChunkShift = RowBits;
        private const int SizeShift = RowBits + ChunkBits;

        private const int RowMask = (1 << RowBits) - 1;
        private const int ChunkMask = (1 << ChunkBits) - 1;
        private const int SizeMask = (1 << SizeBits) - 1;
        private const int ChunkRowMask = (1 << SizeShift) - 1;

        private readonly int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor(int value) => _value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(ChunkSize size, int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert((uint)rowWithinChunk <= RowMask);
            return new Cursor(((int)size << SizeShift) | (chunkIndex << ChunkShift) | rowWithinChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor CreateZero(ChunkSize size) => From(size, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromByteOffset(ChunkSize size, int chunkIndex, int byteOffset)
        {
            Debug.Assert(byteOffset % DbRow.Size == 0, "byteOffset must be row-aligned.");
            return From(size, chunkIndex, byteOffset / DbRow.Size);
        }

        /// <summary>
        /// Gets the packed integer value that encodes this cursor and can be rebuilt
        /// back into a cursor through the value constructor.
        /// </summary>
        public int Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        /// <summary>
        /// Gets the zero-based position of the row within its chunk.
        /// </summary>
        public int Row
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value & RowMask;
        }

        /// <summary>
        /// Gets the zero-based index of the chunk this cursor points into.
        /// </summary>
        public int Chunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value >>> ChunkShift) & ChunkMask;
        }

        /// <summary>
        /// Gets the absolute linear row index across all chunks for this cursor's chunk size.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Chunk * RowsPerChunk) + Row;
        }

        /// <summary>
        /// Gets the byte offset of this cursor's row from the start of its chunk.
        /// </summary>
        public int ByteOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Row * DbRow.Size;
        }

        /// <summary>
        /// Gets a value indicating whether this cursor points at chunk 0.
        /// </summary>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value & ChunkRowMask) == 0;
        }

        /// <summary>
        /// Gets the chunk-size bucket this cursor was minted with.
        /// </summary>
        public ChunkSize ChunkSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChunkSize)((_value >>> SizeShift) & SizeMask);
        }

        /// <summary>
        /// Gets the number of rows a chunk holds for this cursor's chunk size.
        /// </summary>
        public int RowsPerChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (1 << (10 + (int)ChunkSize)) / DbRow.Size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor AddRows(int delta)
        {
            if (delta == 0)
            {
                return this;
            }

            var rowsPerChunk = RowsPerChunk;
            var row = Row + delta;
            var chunk = Chunk;

            if (row >= rowsPerChunk)
            {
                var carry = row / rowsPerChunk;
                row -= carry * rowsPerChunk;
                chunk += carry;
            }
            else if (row < 0)
            {
                var borrow = (-row + rowsPerChunk - 1) / rowsPerChunk;
                row += borrow * rowsPerChunk;
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
                row = rowsPerChunk - 1;
            }

            return From(ChunkSize, chunk, row);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithChunk(int chunk) => From(ChunkSize, chunk, Row);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithRow(int row) => From(ChunkSize, Chunk, row);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Cursor other) => _value == other._value;

        public override bool Equals(object? obj) => obj is Cursor c && Equals(c);

        public override int GetHashCode() => _value;

        public override string ToString() => $"chunk={Chunk}, row={Row}, size={ChunkSize} (0x{_value:X8})";

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
