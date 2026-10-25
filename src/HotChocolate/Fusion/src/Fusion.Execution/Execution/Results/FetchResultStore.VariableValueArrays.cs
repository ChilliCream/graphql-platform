namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    private const int MaxRetainedVariableValueArrays = 32;
    private const int MaxRetainedVariableValueEntries = 256;

    // in lease order, so the arrays leased before the first subscription event come first
    private readonly List<VariableValues[]> _leasedVariableValueArrays = [];

    // oldest first, so the arrays of the last request or event are found and retained first
    private readonly List<VariableValues[]> _freeVariableValueArrays = [];

    // the number of arrays leased before the first subscription event, or -1 before that event
    private int _setupVariableValueArrayCount = -1;

    /// <summary>
    /// Gets an array of exactly <paramref name="length"/> entries that stays valid until the
    /// store is cleaned or, when it was leased for a subscription event, until the next event.
    /// </summary>
    private VariableValues[] RentVariableValueArrayUnsafe(int length)
    {
        var free = _freeVariableValueArrays;
        VariableValues[]? array = null;

        for (var i = free.Count - 1; i >= 0; i--)
        {
            if (free[i].Length == length)
            {
                array = free[i];
                free.RemoveAt(i);
                break;
            }
        }

        array ??= new VariableValues[length];
        _leasedVariableValueArrays.Add(array);
        return array;
    }

    /// <summary>
    /// Returns an array leased for the current request or event that nothing references anymore.
    /// Arrays that this store did not lease for the current request or event are ignored.
    /// </summary>
    private void ReturnVariableValueArrayUnsafe(VariableValues[] array)
    {
        var leased = _leasedVariableValueArrays;
        var start = Math.Max(_setupVariableValueArrayCount, 0);

        for (var i = leased.Count - 1; i >= start; i--)
        {
            if (ReferenceEquals(leased[i], array))
            {
                leased.RemoveAt(i);
                AddFreeVariableValueArray(array);
                return;
            }
        }
    }

    /// <summary>
    /// Makes the arrays leased for the previous subscription event available to the next event.
    /// The arrays leased before the first event stay valid until the store is cleaned.
    /// </summary>
    private void RecycleEventVariableValueArrays()
    {
        if (_setupVariableValueArrayCount < 0)
        {
            _setupVariableValueArrayCount = _leasedVariableValueArrays.Count;
            return;
        }

        RecycleVariableValueArrays(_setupVariableValueArrayCount);
    }

    /// <summary>
    /// Makes every leased array available to the next request.
    /// </summary>
    private void RecycleRequestVariableValueArrays()
    {
        RecycleVariableValueArrays(0);
        _setupVariableValueArrayCount = -1;
    }

    private void ReleaseVariableValueArrays()
    {
        _leasedVariableValueArrays.Clear();
        _freeVariableValueArrays.Clear();
        _setupVariableValueArrayCount = -1;
    }

    private void RecycleVariableValueArrays(int start)
    {
        var leased = _leasedVariableValueArrays;

        if (leased.Count == start)
        {
            return;
        }

        for (var i = start; i < leased.Count; i++)
        {
            AddFreeVariableValueArray(leased[i]);
        }

        leased.RemoveRange(start, leased.Count - start);

        // keep the most recently freed arrays that fit into the retention limits
        var free = _freeVariableValueArrays;
        var retainedEntries = 0;
        var firstRetained = free.Count;

        while (firstRetained > 0
            && free.Count - firstRetained < MaxRetainedVariableValueArrays
            && retainedEntries + free[firstRetained - 1].Length <= MaxRetainedVariableValueEntries)
        {
            firstRetained--;
            retainedEntries += free[firstRetained].Length;
        }

        free.RemoveRange(0, firstRetained);
    }

    private void AddFreeVariableValueArray(VariableValues[] array)
    {
        // clearing unroots the paths and the arena memory the entries point to
        array.AsSpan().Clear();

        if (array.Length <= MaxRetainedVariableValueEntries)
        {
            _freeVariableValueArrays.Add(array);
        }
    }
}
