using System.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// A flat, allocation-free accumulator for additional CompactPath entries
/// that replaces per-slot List&lt;CompactPath&gt; with ArrayPool-rented buffers.
/// Stores (slotIndex, path) pairs and produces <see cref="CompactPathSegment"/>
/// per slot via counting sort in ApplyTo.
/// </summary>
internal ref struct AdditionalPathAccumulator
{
    private CompactPath[]? _paths;
    private int[]? _slotIndices;
    private int _count;

    public readonly bool HasEntries => _count > 0;

    public void Add(int slotIndex, CompactPath path)
    {
        if (_paths is null)
        {
            _paths = ArrayPool<CompactPath>.Shared.Rent(16);
            _slotIndices = ArrayPool<int>.Shared.Rent(16);
        }
        else if (_count == _paths.Length)
        {
            Grow();
        }

        _paths[_count] = path;
        _slotIndices![_count] = slotIndex;
        _count++;
    }

    public void AddRange(int slotIndex, ReadOnlySpan<CompactPath> paths)
    {
        foreach (var path in paths)
        {
            Add(slotIndex, path);
        }
    }

    public void ApplyTo(VariableValues[] variableValueSets, int slotCount)
    {
        if (_count == 0)
        {
            return;
        }

        // Count paths per slot.
        var counts = slotCount <= 256
            ? stackalloc int[slotCount]
            : new int[slotCount];

        for (var i = 0; i < _count; i++)
        {
            counts[_slotIndices![i]]++;
        }

        // Compute start offsets (exclusive prefix sum).
        var offsets = slotCount <= 256
            ? stackalloc int[slotCount]
            : new int[slotCount];

        offsets[0] = 0;
        for (var i = 1; i < slotCount; i++)
        {
            offsets[i] = offsets[i - 1] + counts[i - 1];
        }

        // Scatter paths into a single shared array in sorted order.
        var writePos = slotCount <= 256
            ? stackalloc int[slotCount]
            : new int[slotCount];
        offsets.CopyTo(writePos);

        var shared = new CompactPath[_count];

        for (var i = 0; i < _count; i++)
        {
            var idx = _slotIndices![i];
            shared[writePos[idx]++] = _paths![i];
        }

        // Build CompactPathSegment for each non-empty slot from contiguous slices.
        for (var slot = 0; slot < slotCount; slot++)
        {
            if (counts[slot] == 0)
            {
                continue;
            }

            variableValueSets[slot] = variableValueSets[slot] with
            {
                AdditionalPaths = new CompactPathSegment(shared, offsets[slot], counts[slot])
            };
        }
    }

    private void Grow()
    {
        var newSize = _paths!.Length * 2;

        var newPaths = ArrayPool<CompactPath>.Shared.Rent(newSize);
        _paths.AsSpan(0, _count).CopyTo(newPaths);
        _paths.AsSpan(0, _count).Clear();
        ArrayPool<CompactPath>.Shared.Return(_paths);
        _paths = newPaths;

        var newIndices = ArrayPool<int>.Shared.Rent(newSize);
        _slotIndices.AsSpan(0, _count).CopyTo(newIndices);
        ArrayPool<int>.Shared.Return(_slotIndices!);
        _slotIndices = newIndices;
    }

    public void Dispose()
    {
        if (_paths is not null)
        {
            _paths.AsSpan(0, _count).Clear();
            ArrayPool<CompactPath>.Shared.Return(_paths);
            _paths = null;
        }

        if (_slotIndices is not null)
        {
            ArrayPool<int>.Shared.Return(_slotIndices);
            _slotIndices = null;
        }

        _count = 0;
    }
}
