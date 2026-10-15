using System.Buffers;
using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// A request scoped cache of path segment arrays in front of the shared
/// <see cref="PathSegmentPool"/>. It keeps up to its capacity of arrays between
/// requests and is not thread safe.
/// </summary>
internal sealed class PathSegmentLocalPool : IDisposable
{
    private const int BatchSize = 32;
    private const int BookkeepingRetainFactor = 4;

    private readonly int _capacity;
    private readonly int _freeLength;
    private readonly int _ownedLength;
    private PathSegmentPool _shared;
    private int[]?[] _free;
    private int _freeCount;
    private int[]?[] _owned;
    private int _ownedCount;
    private bool _disposed;

    public PathSegmentLocalPool(int capacity = 64)
        : this(capacity, PathSegmentMemory.Current)
    {
    }

    internal PathSegmentLocalPool(int capacity, PathSegmentPool shared)
    {
        _capacity = Math.Max(1, capacity);
        _freeLength = Math.Max(BatchSize, _capacity);
        _ownedLength = _capacity + BatchSize;
        _shared = shared;
        _free = ArrayPool<int[]?>.Shared.Rent(_freeLength);
        _freeCount = 0;
        _owned = ArrayPool<int[]?>.Shared.Rent(_ownedLength);
        _ownedCount = 0;
    }

    /// <summary>
    /// Gets the number of arrays this pool holds, rented out or free.
    /// </summary>
    internal int OwnedCount => _ownedCount;

    /// <summary>
    /// Gets the number of arrays that can be rented without touching the shared pool.
    /// </summary>
    internal int FreeCount => _freeCount;

    /// <summary>
    /// Gets the number of arrays the owned bookkeeping can hold before it grows.
    /// </summary>
    internal int OwnedCapacity => _owned.Length;

    /// <summary>
    /// Gets the number of arrays the free bookkeeping can hold before it grows.
    /// </summary>
    internal int FreeCapacity => _free.Length;

    /// <summary>
    /// Gets a value that changes with every <see cref="Reset"/> and <see cref="Dispose"/>.
    /// A path created for an earlier value must not rent from this pool.
    /// </summary>
    public int Generation { get; private set; }

    public int[] Rent()
    {
        if (_freeCount == 0)
        {
            RentBatch();
        }

        var array = _free[--_freeCount]!;
        _free[_freeCount] = null;
        return array;
    }

    public void Return(int[] array)
    {
        if (array.Length != PathSegmentMemory.SegmentArraySize)
        {
            return;
        }

        EnsureCapacity(ref _free, _freeCount, _freeCount + 1);
        _free[_freeCount++] = array;
    }

    /// <summary>
    /// Makes every owned array available again and hands the arrays beyond the capacity
    /// back to the shared pool. Arrays of a shared pool that has been replaced are dropped.
    /// Paths created before the reset must no longer be used.
    /// </summary>
    public void Reset()
    {
        Generation++;

        var shared = PathSegmentMemory.Current;

        if (!ReferenceEquals(shared, _shared))
        {
            _owned.AsSpan(0, _ownedCount).Clear();
            _ownedCount = 0;
            _shared = shared;
        }
        else if (_ownedCount > _capacity)
        {
            var excess = _owned.AsSpan(_capacity, _ownedCount - _capacity);
            _shared.Return(excess);
            excess.Clear();
            _ownedCount = _capacity;
        }

        if (_owned.Length > _ownedLength * BookkeepingRetainFactor)
        {
            var replacement = ArrayPool<int[]?>.Shared.Rent(_ownedLength);
            _owned.AsSpan(0, _ownedCount).CopyTo(replacement);
            ArrayPool<int[]?>.Shared.Return(_owned, clearArray: true);
            _owned = replacement;
        }

        if (_free.Length > _freeLength * BookkeepingRetainFactor)
        {
            ArrayPool<int[]?>.Shared.Return(_free, clearArray: true);
            _free = ArrayPool<int[]?>.Shared.Rent(_freeLength);
            _freeCount = 0;
        }

        _owned.AsSpan(0, _ownedCount).CopyTo(_free);

        if (_freeCount > _ownedCount)
        {
            _free.AsSpan(_ownedCount, _freeCount - _ownedCount).Clear();
        }

        _freeCount = _ownedCount;
    }

    private void RentBatch()
    {
        Debug.Assert(_freeCount == 0);

        EnsureCapacity(ref _free, 0, BatchSize);
        EnsureCapacity(ref _owned, _ownedCount, _ownedCount + BatchSize);

        var batch = _free.AsSpan(0, BatchSize);
        _shared.Rent(batch);
        batch.CopyTo(_owned.AsSpan(_ownedCount));
        _ownedCount += BatchSize;
        _freeCount = BatchSize;
    }

    private static void EnsureCapacity(ref int[]?[] array, int count, int required)
    {
        if (array.Length >= required)
        {
            return;
        }

        var replacement = ArrayPool<int[]?>.Shared.Rent(Math.Max(required, array.Length * 2));
        array.AsSpan(0, count).CopyTo(replacement);
        ArrayPool<int[]?>.Shared.Return(array, clearArray: true);
        array = replacement;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Generation++;

        if (ReferenceEquals(_shared, PathSegmentMemory.Current))
        {
            _shared.Return(_owned.AsSpan(0, _ownedCount));
        }

        ArrayPool<int[]?>.Shared.Return(_free, clearArray: true);
        ArrayPool<int[]?>.Shared.Return(_owned, clearArray: true);

        _free = [];
        _freeCount = 0;
        _owned = [];
        _ownedCount = 0;
    }
}
