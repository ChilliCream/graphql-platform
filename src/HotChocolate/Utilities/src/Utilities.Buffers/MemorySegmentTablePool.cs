using System.Buffers;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Numerics;
#endif
using static HotChocolate.Buffers.MemoryArenaEventSource;

namespace HotChocolate.Buffers;

/// <summary>
/// Pools the <see cref="MemorySegment"/>[] tables that arenas rent to track their segments, keyed by
/// length. It keeps enough of each length to cover the tables in use at once, so renting after a
/// return does not allocate.
/// </summary>
internal static class MemorySegmentTablePool
{
    // The smallest and largest bucketed table lengths. Both are powers of two. The two hot lengths
    // proven by the trace are 16 and 64; the neighbours are covered so a slightly larger or smaller
    // rent still lands in a bucket instead of the shared fallback.
    private const int MinLength = 16;
    private const int MaxLength = 128;

    // The number of tables a single bucket retains. The live working set per hot length is on the
    // order of a thousand across concurrent scopes, so each bucket is sized well above that to keep
    // returns from being dropped. Slots are filled lazily, so an unused bucket costs only the empty
    // slot array.
    private const int BucketCapacity = 4096;

    // The first bucket index. Bucket i holds tables of length (MinLength << i).
    private static readonly Bucket[] s_buckets = CreateBuckets();

    private static Bucket[] CreateBuckets()
    {
        var count = BucketIndex(MaxLength) + 1;
        var buckets = new Bucket[count];

        for (var i = 0; i < count; i++)
        {
            buckets[i] = new Bucket(MinLength << i, BucketCapacity);
        }

        return buckets;
    }

    /// <summary>
    /// Rents a <see cref="MemorySegment"/> table of at least <paramref name="minLength"/> entries.
    /// </summary>
    /// <param name="minLength">The minimum number of entries the table must hold.</param>
    /// <returns>A table of at least <paramref name="minLength"/> entries.</returns>
    public static MemorySegment[] Rent(int minLength)
    {
        var length = RoundUpToBucketLength(minLength);

        if (length < MinLength || length > MaxLength)
        {
            // The requested length is outside the bucketed range; the shared pool keeps working for
            // these rare sizes.
            return ArrayPool<MemorySegment>.Shared.Rent(minLength);
        }

        var bucket = s_buckets[BucketIndex(length)];
        var table = bucket.Rent();

        if (table is null)
        {
            table = new MemorySegment[length];

            var log = Log;
            if (log.IsEnabled())
            {
                log.TableAllocated(length);
            }
        }

        return table;
    }

    /// <summary>
    /// Returns a table to the pool, cleared first so it no longer references any page.
    /// </summary>
    /// <param name="table">The table to return.</param>
    public static void Return(MemorySegment[] table)
    {
        var length = table.Length;

        if (length < MinLength || length > MaxLength || !IsPowerOfTwo(length))
        {
            // A table the bucketed range does not cover (for example a shared-pool fallback array)
            // goes straight back to the shared pool, cleared so it does not pin any page.
            ArrayPool<MemorySegment>.Shared.Return(table, clearArray: true);
            return;
        }

        Array.Clear(table, 0, table.Length);
        s_buckets[BucketIndex(length)].Return(table);
    }

    private static int RoundUpToBucketLength(int minLength)
    {
        if (minLength <= MinLength)
        {
            return MinLength;
        }

#if NET8_0_OR_GREATER
        return (int)BitOperations.RoundUpToPowerOf2((uint)minLength);
#else
        var value = (uint)(minLength - 1);
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return (int)(value + 1);
#endif
    }

    private static bool IsPowerOfTwo(int value) => (value & (value - 1)) == 0;

    // The index of the bucket that holds tables of the given power-of-two length.
    private static int BucketIndex(int length)
    {
#if NET8_0_OR_GREATER
        return BitOperations.TrailingZeroCount(length) - BitOperations.TrailingZeroCount(MinLength);
#else
        var index = 0;
        var current = length;
        while (current > MinLength)
        {
            current >>= 1;
            index++;
        }
        return index;
#endif
    }

    // A bounded LIFO stack of tables of one fixed length. Modelled on FixedSizeArrayPool.Bucket: a
    // single SpinLock guards a slot array, slots are filled lazily, and a rent on an empty bucket
    // returns null so the caller can allocate and record the miss.
    private sealed class Bucket
    {
        private readonly int _tableLength;
        private readonly MemorySegment[]?[] _tables;
        private SpinLock _lock;
        private int _index;

        public Bucket(int tableLength, int capacity)
        {
            Debug.Assert(tableLength > 0, "Table length must be positive.");
            Debug.Assert(capacity > 0, "Capacity must be positive.");

            _tableLength = tableLength;
            _tables = new MemorySegment[capacity][];
            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
        }

        public MemorySegment[]? Rent()
        {
            MemorySegment[]? table = null;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    table = _tables[--_index];
                    _tables[_index] = null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return table;
        }

        public void Return(MemorySegment[] table)
        {
            Debug.Assert(
                table.Length == _tableLength,
                "A table returned to a bucket must match the bucket length.");

            var lockTaken = false;
            var dropped = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < _tables.Length)
                {
                    _tables[_index++] = table;
                }
                else
                {
                    // The bucket is full, so the table is dropped on the floor for the GC. The bucket
                    // is sized above the live working set, so this is not expected in steady state; a
                    // drop recorded after warmup means the bucket is undersized for the load.
                    dropped = true;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            if (dropped)
            {
                var log = Log;
                if (log.IsEnabled())
                {
                    log.TableDropped(_tableLength);
                }
            }
        }
    }
}
