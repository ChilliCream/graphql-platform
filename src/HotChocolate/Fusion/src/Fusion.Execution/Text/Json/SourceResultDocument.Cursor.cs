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

        // A cursor holds the chunk index in bits 14 to 25 and the row within the chunk in bits 0 to 13,
        // so cursors compare in (chunk, row) order. The size of a chunk follows from its index.
        public const int MaxChunks = 1 << ChunkBits;

        private const int RowBits = 14;
        private const int ChunkBits = 12;
        private const int ChunkShift = RowBits;

        private const uint RowMask = (1u << RowBits) - 1u;
        private const uint ChunkMask = (1u << ChunkBits) - 1u;

        // The number of rows in all chunks before the chunk at the index, for the chunks up to RampLength.
        private const int RampLength = ChunkSchedule.RampLength;
        private static readonly int[] s_rampPrefix = BuildRampPrefix();

        // The number of rows of the chunk at the index, for the chunks up to RampLength.
        private static readonly int[] s_rowsPerChunk = BuildRowsPerChunk();

        private readonly uint _value;

        static Cursor()
        {
            Debug.Assert(DbRowSize > 0, "Row size must be > 0");
            Debug.Assert(MaxRowsPerChunk <= (int)(RowMask + 1), "RowBits too small for RowsPerChunk");
            Debug.Assert(MaxChunks <= (int)(ChunkMask + 1), "ChunkBits too small for MaxChunks");
        }

        public static readonly Cursor Zero = From(0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Cursor(uint value) => _value = value;

        private const int MaxRowsPerChunk = (1 << (10 + (int)ChunkSize.Size128K)) / DbRowSize;

        /// <summary>
        /// Gets the chunk-size bucket for the given chunk index following the geometric schedule.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkSize ChunkSizeFor(int chunkIndex)
            => ChunkSchedule.SizeFor(chunkIndex);

        /// <summary>
        /// Gets the number of rows the chunk at the given index holds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RowsPerChunkFor(int chunkIndex)
            => s_rowsPerChunk[Math.Min(chunkIndex, RampLength)];

        /// <summary>
        /// Create from validated (chunk,row) parts.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor From(int chunkIndex, int rowWithinChunk)
        {
            Debug.Assert((uint)chunkIndex < MaxChunks);
            Debug.Assert((uint)rowWithinChunk <= RowMask);
            return new Cursor(((uint)chunkIndex << ChunkShift) | (uint)rowWithinChunk);
        }

        /// <summary>
        /// Creates the cursor of the first row of the chunk that follows the chunk of
        /// <paramref name="cursor"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The chunk of <paramref name="cursor"/> is the last chunk that a cursor can address.
        /// </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Cursor FirstOfNextChunk(Cursor cursor)
        {
            var chunkIndex = cursor.Chunk + 1;

            if (chunkIndex >= MaxChunks)
            {
                throw ThrowHelper.SourceResultDocumentTooLarge();
            }

            return From(chunkIndex, 0);
        }

        /// <summary>
        /// Try create without asserts; false if out of range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFrom(int chunkIndex, int rowWithinChunk, out Cursor idx)
        {
            if ((uint)chunkIndex >= MaxChunks || rowWithinChunk >= RowsPerChunkFor(chunkIndex))
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

        /// <summary>
        /// Creates a cursor from its packed <see cref="Value"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Cursor FromValue(uint value) => new(value);

        public uint Value => _value;

        public int Chunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_value >> ChunkShift) & ChunkMask);
        }

        /// <summary>
        /// Row index within the chunk (0..RowsPerChunkFor(chunk)-1).
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

        /// <summary>
        /// Gets the number of rows the chunk this cursor points into holds.
        /// </summary>
        public int RowsPerChunk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsPerChunkFor(Chunk);
        }

        /// <summary>
        /// Gets the absolute linear row index across all chunks following the geometric schedule.
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RowsBeforeChunk(Chunk) + Row;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor AddRows(int delta)
        {
            var chunk = Chunk;
            var row = Row + delta;

            // Fast path: the move stays inside the current chunk (the common +1/-1 step). One table
            // load gives the chunk capacity and a single unsigned compare covers both bounds.
            if ((uint)row < (uint)s_rowsPerChunk[Math.Min(chunk, RampLength)])
            {
                return From(chunk, row);
            }

            return AddRowsAcrossChunks(this, delta);
        }

        /// <summary>
        /// Moves <paramref name="cursor"/> by <paramref name="delta"/> rows when the target row lies
        /// outside the current chunk.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Cursor AddRowsAcrossChunks(Cursor cursor, int delta)
        {
            if (delta == 0)
            {
                return cursor;
            }

            var chunk = cursor.Chunk;
            var nextChunk = chunk + 1;

            if (delta > 0 && nextChunk < MaxChunks)
            {
                var nextRow = cursor.Row + delta - s_rowsPerChunk[Math.Min(chunk, RampLength)];

                if ((uint)nextRow < (uint)s_rowsPerChunk[Math.Min(nextChunk, RampLength)])
                {
                    return From(nextChunk, nextRow);
                }
            }

            // Project onto the linear row index and map it back to (chunk, row) in O(1). The ramp
            // is a short prefix scan; the constant tail is closed-form division.
            var linear = RowsBeforeChunk(chunk) + cursor.Row + delta;

            if (linear < 0)
            {
                Debug.Fail("Cursor underflow");
                return From(0, 0);
            }

            return FromLinear(linear);
        }

        /// <summary>
        /// Maps an absolute linear row index back to a (chunk, row) cursor following the geometric
        /// schedule. Within the ramp this is a short prefix scan; past the ramp it is closed-form.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Cursor FromLinear(int linear)
        {
            if (linear >= s_rampPrefix[RampLength])
            {
                var rem = linear - s_rampPrefix[RampLength];
                var carry = rem / MaxRowsPerChunk;
                var chunk = RampLength + carry;
                var row = rem - (carry * MaxRowsPerChunk);

                if (chunk >= MaxChunks)
                {
                    Debug.Fail("Cursor overflow");
                    chunk = MaxChunks - 1;
                    row = MaxRowsPerChunk - 1;
                }

                return From(chunk, row);
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

            return From(c, linear - s_rampPrefix[c]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithChunk(int chunk) => From(chunk, Row);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Cursor WithRow(int row) => From(Chunk, row);

        /// <summary>
        /// Gets the number of rows in all chunks before the chunk at the given index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RowsBeforeChunk(int chunk)
        {
            if (chunk <= RampLength)
            {
                return s_rampPrefix[chunk];
            }

            return s_rampPrefix[RampLength] + ((chunk - RampLength) * MaxRowsPerChunk);
        }

        private static int[] BuildRampPrefix()
        {
            var prefix = new int[RampLength + 1];
            var cumulative = 0;

            for (var chunk = 0; chunk < RampLength; chunk++)
            {
                prefix[chunk] = cumulative;
                cumulative += ComputeRowsPerChunk(chunk);
            }

            prefix[RampLength] = cumulative;
            return prefix;
        }

        private static int[] BuildRowsPerChunk()
        {
            var rows = new int[RampLength + 1];

            for (var chunk = 0; chunk <= RampLength; chunk++)
            {
                rows[chunk] = ComputeRowsPerChunk(chunk);
            }

            return rows;
        }

        private static int ComputeRowsPerChunk(int chunkIndex)
            => (1 << (10 + (int)ChunkSizeFor(chunkIndex))) / DbRowSize;

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
