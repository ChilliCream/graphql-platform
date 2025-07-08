using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultDataBatch<T> where T : ResultData, new()
{
    private readonly int _defaultCapacity;
    private readonly int _maxAllowedCapacity;
    private readonly T[] _items;
    private int _next = -1;

    public ResultDataBatch(int batchSize, int defaultCapacity = 8, int maxAllowedCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultCapacity, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllowedCapacity, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultCapacity, maxAllowedCapacity);

        _defaultCapacity = defaultCapacity;
        _maxAllowedCapacity = maxAllowedCapacity;
        _items = new T[batchSize];

        for (var i = 0; i < batchSize; i++)
        {
            _items[i] = CreateItem();
        }
    }

    private T CreateItem()
    {
        var item = new T();
        item.SetCapacity(_defaultCapacity, _maxAllowedCapacity);
        return item;
    }

    public bool TryRent([NotNullWhen(true)] out T? item)
    {
        if (_next >= _items.Length)
        {
            item = null;
            return false;
        }

        var index = Interlocked.Increment(ref _next);

        if (index < _items.Length)
        {
            item = _items[index];
            return true;
        }

        item = null;
        return false;
    }

    public void Reset()
    {
        if (_next == -1)
        {
            return;
        }

        var usedItem = _next < _items.Length ? _next : _items.Length;
        ref var item = ref MemoryMarshal.GetReference(_items.AsSpan());
        ref var end = ref Unsafe.Add(ref item, usedItem);

        while (Unsafe.IsAddressLessThan(ref item, ref end))
        {
            if (!item.Reset())
            {
                item = CreateItem();
            }
            item = ref Unsafe.Add(ref item, 1)!;
        }

        _next = -1;
    }
}
