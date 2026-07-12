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
        // The cursor packs (chunk, row) into a fixed split of the low 26 bits: the high 13 bits
        // hold the chunk index and the low 13 bits hold the row within the chunk. The chunk-size
        // bucket is not stored; it is derived from the chunk index through the geometric growth
        // schedule (chunk i has ordinal Min(i, 7), so chunks 0..7 ramp Size1K..Size128K and every
        // later chunk stays at Size128K). A 128K chunk holds 131072/20 = 6553 rows, which fits the
        // 13-bit row field; 13 chunk bits give 8192 chunks, so the total capacity (~53M rows)
        // exceeds the former fixed 128K layout. Keeping chunk in the high bits makes the raw value
        // compare in linear (chunk, row) order, and the 26-bit value still fits the 29-bit DbRow
        // fields it is stored in.
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

        // Rows held by the chunk at ordinal i (i = Min(chunk, RampLength)); a direct table load
        // replaces the Min+shift+div of RowsPerChunkFor on the AddRows fast path.
        private static readonly int[] s_rowsPerChunk = BuildRowsPerChunk();

        // The packed value immediately after the last addressable row. This remains inside the
        // 13-bit row field and is used as the one-past-the-end cursor by appenders and enumerators.
        private static readonly Cursor s_end = PackUnchecked(
            MaxChunks - 1,
            RowsPerChunkFor(MaxChunks - 1));

        private readonly int _value;

        /// <summary>
        /// Rebuilds a cursor from a value previously read out of a DbRow field.
        /// </summary>
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

        /// <summary>
        /// Creates a cursor from a chunk index and a row within the chunk.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(int chunkIndex, int rowWithinChunk)
        {
            if ((uint)chunkIndex >= MaxChunks)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkIndex));
            }

            if ((uint)rowWithinChunk >= (uint)RowsPerChunkFor(chunkIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(rowWithinChunk));
            }

            return PackUnchecked(chunkIndex, rowWithinChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Cursor PackUnchecked(int chunkIndex, int rowWithinChunk)
            => new((chunkIndex << ChunkShift) | rowWithinChunk);

        /// <summary>
        /// Creates the zero cursor (chunk 0, row 0).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor CreateZero() => new(0);

        /// <summary>
        /// Gets the one-past-the-end cursor for the complete representable metadata space.
        /// </summary>
        public static Cursor End => s_end;

        /// <summary>
        /// Creates a cursor for a row-aligned byte offset within the given chunk.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromByteOffset(int chunkIndex, int byteOffset)
        {
            if (byteOffset < 0 || byteOffset % DbRow.Size != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteOffset));
            }

            return From(chunkIndex, byteOffset / DbRow.Size);
        }

        /// <summary>
        /// Gets the raw packed value (chunk and row).
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Gets the chunk-size bucket this cursor's chunk uses.
        /// </summary>
        public ChunkSize ChunkSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ChunkSizeFor(Chunk);
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
        /// Gets a value indicating whether this cursor addresses chunk 0, row 0.
        /// </summary>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value == 0;
        }

        /// <summary>
        /// Gets the number of rows the chunk this cursor points into holds.
        /// </summary>
        public int RowsPerChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsPerChunkFor(Chunk);
        }

        /// <summary>
        /// Gets the linear row identifier across all chunks following the geometric schedule.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsBeforeChunk(Chunk) + Row;
        }

        /// <summary>
        /// Advances (or rewinds) the cursor by the given number of rows, carrying or
        /// borrowing across the geometric chunk boundaries.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor AddRows(int delta)
        {
            if (delta == 0)
            {
                return this;
            }

            var chunk = Chunk;
            var row = Row + delta;

            // Fast path: the move stays inside the current chunk (the common +1/-1 step). One table
            // load gives the chunk capacity and a single unsigned compare covers both bounds.
            if ((uint)row < (uint)s_rowsPerChunk[Math.Min(chunk, RampLength)])
            {
                return PackUnchecked(chunk, row);
            }

            // General path: project onto the linear row index and map it back to (chunk, row) in
            // O(1). The ramp is a short prefix scan; the constant tail is closed-form division.
            var linear = (long)RowsBeforeChunk(chunk) + Row + delta;

            if ((ulong)linear > (ulong)End.Index)
            {
                throw new OverflowException("Cursor movement exceeds the representable metadata range.");
            }

            return FromLinear((int)linear);
        }

        /// <summary>
        /// Maps an absolute linear row index back to a (chunk, row) cursor following the geometric
        /// schedule. Within the ramp this is a short prefix scan; past the ramp it is closed-form.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Cursor FromLinear(int linear)
        {
            if (linear == End.Index)
            {
                return End;
            }

            if (linear >= s_rampPrefix[RampLength])
            {
                const int maxRowsPerChunk = (1 << (10 + (int)ChunkSize.Size128K)) / DbRow.Size;
                var rem = linear - s_rampPrefix[RampLength];
                var carry = rem / maxRowsPerChunk;
                var chunk = RampLength + carry;
                var row = rem - (carry * maxRowsPerChunk);

                return PackUnchecked(chunk, row);
            }

            // Ramp: locate the chunk whose prefix window contains the linear index. RampLength is a
            // small constant, so this resolves in a fixed number of compares.
            var c = RampLength - 1;

            for (var i = 0; i < RampLength; i++)
            {
                if (s_rampPrefix[i + 1] > linear)
                {
                    c = i;
                    break;
                }
            }

            return PackUnchecked(c, linear - s_rampPrefix[c]);
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

        private static int[] BuildRowsPerChunk()
        {
            var rows = new int[RampLength + 1];

            for (var ordinal = 0; ordinal <= RampLength; ordinal++)
            {
                rows[ordinal] = RowsPerChunkFor(ordinal);
            }

            return rows;
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
