using System.Buffers;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class VariableDedupTable(ChunkedArrayWriter writer) : IDisposable
{
    private const int DefaultBucketSize = 4;
    private const int DefaultBucketCount = 16;

    private readonly ChunkedArrayWriter _writer = writer;
    private Entry[] _table = ArrayPool<Entry>.Shared.Rent(DefaultBucketCount * DefaultBucketSize);
    private int _bucketCount = DefaultBucketCount;
    private readonly int _bucketSize = DefaultBucketSize;

    public void Initialize(int capacity)
    {
        _bucketCount = NextPowerOfTwo(Math.Max(capacity, DefaultBucketCount));
        var totalSize = _bucketCount * _bucketSize;

        if (_table.Length < totalSize)
        {
            ArrayPool<Entry>.Shared.Return(_table);
            _table = ArrayPool<Entry>.Shared.Rent(totalSize);
        }

        _table.AsSpan(0, totalSize).Clear();
    }

    public bool TryGet(
        int hash,
        int location,
        int length,
        out int existingIndex)
    {
        var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
        var start = bucket * _bucketSize;
        var end = start + _bucketSize;

        for (var s = start; s < end; s++)
        {
            ref var entry = ref _table[s];

            if (entry.Index == 0)
            {
                existingIndex = -1;
                return false;
            }

            if (entry.Hash == hash
                && entry.Length == length
                && _writer.SequenceEqual(entry.Location, location, length))
            {
                existingIndex = entry.Index - 1;
                return true;
            }
        }

        existingIndex = -1;
        return false;
    }

    public void Add(int hash, int index, int location, int length)
    {
        var bucket = hash & 0x7FFFFFFF & (_bucketCount - 1);
        var start = bucket * _bucketSize;
        var end = start + _bucketSize;

        for (var s = start; s < end; s++)
        {
            ref var entry = ref _table[s];

            if (entry.Index == 0)
            {
                entry.Hash = hash;
                entry.Index = index + 1;
                entry.Location = location;
                entry.Length = length;
                return;
            }
        }

        Grow();
        Add(hash, index, location, length);
    }

    public void Clear()
        => _table.AsSpan(0, _bucketCount * _bucketSize).Clear();

    public void Dispose()
    {
        ArrayPool<Entry>.Shared.Return(_table);
        _table = [];
    }

    private void Grow()
    {
        var oldTable = _table;
        var oldTotal = _bucketCount * _bucketSize;

        _bucketCount *= 2;
        var newTotal = _bucketCount * _bucketSize;
        _table = ArrayPool<Entry>.Shared.Rent(newTotal);
        _table.AsSpan(0, newTotal).Clear();

        for (var i = 0; i < oldTotal; i++)
        {
            var entry = oldTable[i];

            if (entry.Index != 0)
            {
                Add(entry.Hash, entry.Index - 1, entry.Location, entry.Length);
            }
        }

        ArrayPool<Entry>.Shared.Return(oldTable);
    }

    private static int NextPowerOfTwo(int n)
    {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        return n + 1;
    }

    private struct Entry
    {
        public int Hash;
        public int Index;    // 1-based (0 = empty)
        public int Location;
        public int Length;
    }
}
