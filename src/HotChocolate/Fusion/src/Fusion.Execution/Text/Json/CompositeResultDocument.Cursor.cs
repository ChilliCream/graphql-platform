using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    /// <summary>
    /// Comparable MetaDb cursor (chunk,row) for CompositeResultDocument.
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
        private const int ChunkAndRowMask = (1 << SizeShift) - 1;

        private readonly int _value;

        /// <summary>
        /// Rebuilds a cursor from a value previously read out of a DbRow field.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor(int value) => _value = value;

        /// <summary>
        /// Creates a cursor stamped with the given chunk-size bucket.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(ChunkSize size, int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert(rowWithinChunk >= 0 && rowWithinChunk <= RowMask);
            return new Cursor(((int)size << SizeShift) | (chunkIndex << ChunkShift) | rowWithinChunk);
        }

        /// <summary>
        /// Creates the zero cursor (chunk 0, row 0) for the given chunk-size bucket.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor CreateZero(ChunkSize size) => From(size, 0, 0);

        /// <summary>
        /// Creates a cursor for a row-aligned byte offset, preserving this cursor's size bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor FromByteOffset(int chunkIndex, int byteOffset)
        {
            Debug.Assert(byteOffset % DbRow.Size == 0, "byteOffset must be row-aligned.");
            return From(ChunkSize, chunkIndex, byteOffset / DbRow.Size);
        }

        /// <summary>
        /// Gets the raw packed value (size, chunk and row).
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Gets the chunk-size bucket this cursor was stamped with.
        /// </summary>
        public ChunkSize ChunkSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChunkSize)((_value >>> SizeShift) & SizeMask);
        }

        /// <summary>
        /// Gets the chunk index.
        /// </summary>
        public int Chunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value >>> ChunkShift) & ChunkMask;
        }

        /// <summary>
        /// Gets the row within the chunk.
        /// </summary>
        public int Row
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value & RowMask;
        }

        /// <summary>
        /// Gets the byte offset of the row within its chunk.
        /// </summary>
        public int ByteOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Row * DbRow.Size;
        }

        /// <summary>
        /// Gets a value indicating whether this cursor addresses chunk 0, row 0,
        /// regardless of the stamped chunk-size bucket.
        /// </summary>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value & ChunkAndRowMask) == 0;
        }

        /// <summary>
        /// Gets the number of rows that fit in a chunk of this cursor's size bucket.
        /// </summary>
        public int RowsPerChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (1 << (10 + (int)ChunkSize)) / DbRow.Size;
        }

        /// <summary>
        /// Gets the linear row identifier across all chunks for this cursor's size bucket.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Chunk * RowsPerChunk) + Row;
        }

        /// <summary>
        /// Advances (or rewinds) the cursor by the given number of rows, carrying or
        /// borrowing across chunk boundaries and preserving the stamped size bits.
        /// </summary>
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
        public bool Equals(Cursor other) => _value == other._value;

        public override bool Equals(object? obj) => obj is Cursor c && Equals(c);

        public override int GetHashCode() => _value;

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
