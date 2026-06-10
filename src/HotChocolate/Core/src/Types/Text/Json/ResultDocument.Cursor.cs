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
        // The cursor packs (chunk, row) into a fixed split of the low 26 bits: the high 13 bits
        // hold the chunk index and the low 13 bits hold the row within the chunk. The chunk-size
        // bucket is not stored; it is derived from the chunk index through the geometric growth
        // schedule (chunk i has ordinal Min(i, 7)). Keeping chunk in the high bits makes the raw
        // value compare in linear (chunk, row) order.
        public const int MaxChunks = 1 << ChunkBits;

        private const int RowBits = 13;
        private const int ChunkBits = 13;
        private const int ChunkShift = RowBits;

        private const int RowMask = (1 << RowBits) - 1;
        private const int ChunkMask = (1 << ChunkBits) - 1;

        // The geometric ramp covers chunks 0..7 (Size1K..Size128K); chunk 8 and beyond stay at
        // Size128K. s_rampPrefix[c] is the cumulative number of rows held by all chunks before
        // chunk c, derived from RowsPerChunkFor for c = 0..RampLength.
        private const int RampLength = (int)ChunkSize.Size128K;
        private static readonly int[] s_rampPrefix = BuildRampPrefix();

        private readonly int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor(int value) => _value = value;

        /// <summary>
        /// Gets the chunk-size bucket for the given chunk index following the geometric schedule.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkSize ChunkSizeFor(int chunkIndex)
            => (ChunkSize)Math.Min(chunkIndex, (int)ChunkSize.Size128K);

        /// <summary>
        /// Gets the number of rows the chunk at the given index holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RowsPerChunkFor(int chunkIndex)
            => (1 << (10 + (int)ChunkSizeFor(chunkIndex))) / DbRow.Size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert((uint)rowWithinChunk <= RowMask);
            return new Cursor((chunkIndex << ChunkShift) | rowWithinChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor CreateZero() => new(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromByteOffset(int chunkIndex, int byteOffset)
        {
            Debug.Assert(byteOffset % DbRow.Size == 0, "byteOffset must be row-aligned.");
            return From(chunkIndex, byteOffset / DbRow.Size);
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
        /// Gets the absolute linear row index across all chunks following the geometric schedule.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsBeforeChunk(Chunk) + Row;
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
        /// Gets a value indicating whether this cursor points at chunk 0, row 0.
        /// </summary>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value == 0;
        }

        /// <summary>
        /// Gets the chunk-size bucket this cursor's chunk uses.
        /// </summary>
        public ChunkSize ChunkSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ChunkSizeFor(Chunk);
        }

        /// <summary>
        /// Gets the number of rows the chunk this cursor points into holds.
        /// </summary>
        public int RowsPerChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsPerChunkFor(Chunk);
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

            // Roll forward across one or more variable-size chunks. The per-step capacity follows
            // the geometric schedule, so the loop walks chunk by chunk until the row fits.
            while (row >= RowsPerChunkFor(chunk))
            {
                row -= RowsPerChunkFor(chunk);
                chunk++;
            }

            // Roll backward across one or more variable-size chunks.
            while (row < 0)
            {
                chunk--;

                if (chunk < 0)
                {
                    Debug.Fail("Cursor underflow");
                    return new Cursor(0);
                }

                row += RowsPerChunkFor(chunk);
            }

            if (chunk >= MaxChunks)
            {
                Debug.Fail("Cursor overflow");
                chunk = MaxChunks - 1;
                row = RowsPerChunkFor(chunk) - 1;
            }

            return From(chunk, row);
        }

        /// <summary>
        /// Gets the cumulative number of rows held by all chunks before the given chunk index,
        /// following the geometric schedule. The ramp (chunks 0..7) is a small prefix table; from
        /// chunk 8 on every chunk holds the same number of rows, so the tail is closed-form.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RowsBeforeChunk(int chunk)
        {
            if (chunk <= RampLength)
            {
                return s_rampPrefix[chunk];
            }

            const int maxRowsPerChunk = (1 << (10 + (int)ChunkSize.Size128K)) / DbRow.Size;
            return s_rampPrefix[RampLength] + ((chunk - RampLength) * maxRowsPerChunk);
        }

        private static int[] BuildRampPrefix()
        {
            var prefix = new int[RampLength + 1];
            var cumulative = 0;

            for (var chunk = 0; chunk < RampLength; chunk++)
            {
                prefix[chunk] = cumulative;
                cumulative += RowsPerChunkFor(chunk);
            }

            prefix[RampLength] = cumulative;
            return prefix;
        }

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
