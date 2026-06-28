using System.Diagnostics;

namespace HotChocolate.Buffers;

/// <summary>
/// Pools the arrays an arena rents to track its tables, so it can return them when it seals.
/// </summary>
internal static class MemorySegmentTableTrackerPool
{
    // A tracker holds one slot per rented table. The capacity is sized above the typical per-request
    // table count (the deep query rents on the order of forty), so typical requests never grow past
    // a pooled tracker; a request that needs more grows into a plain array that is dropped, not
    // pooled, which keeps the bucket holding only fixed-capacity trackers.
    private const int TrackerCapacity = 48;

    // One tracker is live per open arena, so the bucket retains more than the concurrent arena count
    // to keep returns from being dropped. Slots fill lazily, so an unused slot costs nothing beyond
    // the empty slot array.
    private const int BucketCapacity = 4096;

    private static readonly Bucket s_bucket = new(TrackerCapacity, BucketCapacity);

    /// <summary>
    /// Gets the number of table slots a pooled tracker holds.
    /// </summary>
    public static int Capacity => TrackerCapacity;

    /// <summary>
    /// Rents a tracker with <see cref="Capacity"/> cleared table slots.
    /// </summary>
    public static MemorySegment[][] Rent() => s_bucket.Rent() ?? new MemorySegment[TrackerCapacity][];

    /// <summary>
    /// Returns a tracker to the pool, cleared first so it no longer references any table.
    /// </summary>
    /// <param name="tracker">The tracker to return.</param>
    /// <param name="usedLength">The number of slots that were in use.</param>
    public static void Return(MemorySegment[][] tracker, int usedLength)
    {
        if (tracker.Length != TrackerCapacity)
        {
            // A grown fallback tracker does not match the bucket length, so it is dropped on the
            // floor for the GC rather than parked.
            return;
        }

        Array.Clear(tracker, 0, usedLength);
        s_bucket.Return(tracker);
    }

    // A bounded LIFO stack of trackers of one fixed capacity. Modelled on
    // MemorySegmentTablePool.Bucket: a single SpinLock guards a slot array, slots are filled
    // lazily, and a rent on an empty bucket returns null so the caller can allocate.
    private sealed class Bucket
    {
        private readonly int _trackerCapacity;
        private readonly MemorySegment[][]?[] _trackers;
        private SpinLock _lock;
        private int _index;

        public Bucket(int trackerCapacity, int capacity)
        {
            Debug.Assert(trackerCapacity > 0, "Tracker capacity must be positive.");
            Debug.Assert(capacity > 0, "Capacity must be positive.");

            _trackerCapacity = trackerCapacity;
            _trackers = new MemorySegment[capacity][][];
            _lock = new SpinLock(Debugger.IsAttached);
            _index = 0;
        }

        public MemorySegment[][]? Rent()
        {
            MemorySegment[][]? tracker = null;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index > 0)
                {
                    tracker = _trackers[--_index];
                    _trackers[_index] = null;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }

            return tracker;
        }

        public void Return(MemorySegment[][] tracker)
        {
            Debug.Assert(
                tracker.Length == _trackerCapacity,
                "A tracker returned to the bucket must match the bucket capacity.");

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_index < _trackers.Length)
                {
                    _trackers[_index++] = tracker;
                }

                // The bucket is sized above the concurrent arena count, so a full bucket is not
                // expected in steady state; an over-capacity tracker is dropped for the GC.
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit(false);
                }
            }
        }
    }
}
