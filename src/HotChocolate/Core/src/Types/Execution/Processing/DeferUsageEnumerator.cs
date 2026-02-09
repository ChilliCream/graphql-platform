using System.Collections;
using System.Diagnostics;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// An enumerable and enumerator for active defer usages.
/// </summary>
[DebuggerDisplay("{Current,nq}")]
public struct DeferUsageEnumerator : IEnumerable<DeferUsage>, IEnumerator<DeferUsage>
{
    private readonly DeferUsage[] _deferUsages;
    private readonly ulong _deferFlags;
    private int _index;

    internal DeferUsageEnumerator(DeferUsage[] deferUsages, ulong deferFlags)
    {
        _deferUsages = deferUsages;
        _deferFlags = deferFlags;
        _index = -1;
    }

    /// <inheritdoc />
    public DeferUsage Current => _deferUsages[_index];

    /// <inheritdoc />
    object IEnumerator.Current => Current;

    /// <summary>
    /// Returns an enumerator that iterates through active defer usages.
    /// </summary>
    public DeferUsageEnumerator GetEnumerator()
    {
        var enumerator = this;
        enumerator._index = -1;
        return enumerator;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc />
    IEnumerator<DeferUsage> IEnumerable<DeferUsage>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc />
    public bool MoveNext()
    {
        var usages = _deferUsages;
        var flags = _deferFlags;

        if (usages.Length == 0)
        {
            return false;
        }

        while (++_index < usages.Length)
        {
            var usage = usages[_index];
            var bit = 1UL << usage.DeferConditionIndex;

            if ((flags & bit) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Reset() => _index = -1;

    /// <inheritdoc />
    public void Dispose() => _index = _deferUsages.Length;
}
