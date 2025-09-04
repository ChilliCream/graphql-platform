using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static HotChocolate.Fusion.Execution.Results.ResultPoolEventSource;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed class ResultDataBatch<T> where T : ResultData, new()
{
    private static readonly string s_typeName = typeof(T).Name;
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
            _items[i] = CreateItem(ObjectRecreationReason.NewBatch);
        }
    }

    private T CreateItem(ObjectRecreationReason reason)
    {
        Log.ObjectCreated(s_typeName, reason);

        var item = new T();
        item.SetCapacity(_defaultCapacity, _maxAllowedCapacity);
        return item;
    }

    public bool TryRent([NotNullWhen(true)] out T? item)
    {
        // Theoretically the increment could overflow _next and would then
        // become negative as interlocked is unchecked.
        // Which would cause consecutive errors.
        //
        // However, the ResultDataPoolSession would rent another
        // batch when it detects exhaustion of the batch.
        // Which means we will never end up in that error state.
        //
        // When this batch is reset the _next value
        // is set to the initial state, even if we have
        // over shot here, reset will amend the issue.
        var index = Interlocked.Increment(ref _next);

        if (index < _items.Length)
        {
            item = _items[index];
            Log.ObjectRented(s_typeName, index);
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

        var objectsRecreated = 0;
        var usedItems = _next < _items.Length ? _next + 1 : _items.Length;
        ref var item = ref MemoryMarshal.GetReference(_items.AsSpan());
        ref var end = ref Unsafe.Add(ref item, usedItems);

        var utilizationPercentage = usedItems * 100 / _items.Length;
        Log.BatchUtilization(s_typeName, _items.Length, utilizationPercentage);

        while (Unsafe.IsAddressLessThan(ref item, ref end))
        {
            if (!item.Reset())
            {
                item = CreateItem(ObjectRecreationReason.ResetFailed);
                objectsRecreated++;
            }
            item = ref Unsafe.Add(ref item, 1)!;
        }

        Log.BatchReset(typeof(T).Name, usedItems, objectsRecreated);

        _next = -1;
    }
}
