using System.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// A flat, allocation-free accumulator for additional targets that replaces per-slot
/// List&lt;CompactPath&gt; with ArrayPool-rented buffers.
/// Stores (slotIndex, target) pairs and produces <see cref="CompactPathSegment"/>
/// per slot via counting sort in ApplyTo. A target is either a path or a result element; when
/// every target is a result element, the segments keep cursors and create paths on first access.
/// </summary>
internal ref struct AdditionalPathAccumulator
{
    private const int NoCursor = -1;

    private CompactPath[]? _paths;
    private int[]? _cursors;
    private int[]? _slotIndices;
    private int _count;
    private int _pathCount;

    public readonly bool HasEntries => _count > 0;

    public void Add(int slotIndex, CompactPath path)
    {
        var index = Reserve(slotIndex);
        _paths![index] = path;
        _cursors![index] = NoCursor;
        _pathCount++;
    }

    public void Add(int slotIndex, CompositeResultElement target)
    {
        var index = Reserve(slotIndex);
        _cursors![index] = target.Cursor.Value;
    }

    public void AddRange(int slotIndex, ReadOnlySpan<CompactPath> paths)
    {
        foreach (var path in paths)
        {
            Add(slotIndex, path);
        }
    }

    public void ApplyTo(
        VariableValues[] variableValueSets,
        int slotCount,
        FetchResultStore store,
        CompositeResultDocument document)
    {
        if (_count == 0)
        {
            return;
        }

        var segmentBuffer = ArrayPool<CompactPathSegment>.Shared.Rent(slotCount);
        var segments = segmentBuffer.AsSpan(0, slotCount);
        segments.Clear();
        CreateSegments(segments, store, document);

        for (var slot = 0; slot < slotCount; slot++)
        {
            if (!segments[slot].IsDefaultOrEmpty)
            {
                variableValueSets[slot] = variableValueSets[slot] with { AdditionalPaths = segments[slot] };
            }
        }

        segments.Clear();
        ArrayPool<CompactPathSegment>.Shared.Return(segmentBuffer);
    }

    public void ApplyTo(
        EntityResultPath[] resultPaths,
        int slotCount,
        FetchResultStore store,
        CompositeResultDocument document)
    {
        if (_count == 0)
        {
            return;
        }

        var segmentBuffer = ArrayPool<CompactPathSegment>.Shared.Rent(slotCount);
        var segments = segmentBuffer.AsSpan(0, slotCount);
        segments.Clear();
        CreateSegments(segments, store, document);

        for (var slot = 0; slot < slotCount; slot++)
        {
            if (!segments[slot].IsDefaultOrEmpty)
            {
                resultPaths[slot] = resultPaths[slot] with { AdditionalPaths = segments[slot] };
            }
        }

        segments.Clear();
        ArrayPool<CompactPathSegment>.Shared.Return(segmentBuffer);
    }

    private readonly void CreateSegments(
        Span<CompactPathSegment> segments,
        FetchResultStore store,
        CompositeResultDocument document)
    {
        var slotCount = segments.Length;

        // Count targets per slot.
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

        // Scatter targets into a single shared array in sorted order.
        var writePos = slotCount <= 256
            ? stackalloc int[slotCount]
            : new int[slotCount];
        offsets.CopyTo(writePos);

        if (_pathCount == 0)
        {
            var cursors = new int[_count];

            for (var i = 0; i < _count; i++)
            {
                cursors[writePos[_slotIndices![i]]++] = _cursors![i];
            }

            var source = new TargetCursorPaths(store, document, cursors);

            for (var slot = 0; slot < slotCount; slot++)
            {
                if (counts[slot] > 0)
                {
                    segments[slot] = new CompactPathSegment(source, offsets[slot], counts[slot]);
                }
            }

            return;
        }

        var shared = new CompactPath[_count];

        for (var i = 0; i < _count; i++)
        {
            shared[writePos[_slotIndices![i]]++] = GetPath(i, document);
        }

        for (var slot = 0; slot < slotCount; slot++)
        {
            if (counts[slot] > 0)
            {
                segments[slot] = new CompactPathSegment(shared, offsets[slot], counts[slot]);
            }
        }
    }

    private readonly CompactPath GetPath(int index, CompositeResultDocument document)
        => _cursors![index] == NoCursor
            ? _paths![index]
            : document.CreateCompactPath(new CompositeResultDocument.Cursor(_cursors[index]));

    private int Reserve(int slotIndex)
    {
        if (_paths is null)
        {
            _paths = ArrayPool<CompactPath>.Shared.Rent(16);
            _cursors = ArrayPool<int>.Shared.Rent(16);
            _slotIndices = ArrayPool<int>.Shared.Rent(16);
        }
        else if (_count == _paths.Length)
        {
            Grow();
        }

        _slotIndices![_count] = slotIndex;
        return _count++;
    }

    private void Grow()
    {
        var newSize = _paths!.Length * 2;

        var newPaths = ArrayPool<CompactPath>.Shared.Rent(newSize);
        _paths.AsSpan(0, _count).CopyTo(newPaths);
        _paths.AsSpan(0, _count).Clear();
        ArrayPool<CompactPath>.Shared.Return(_paths);
        _paths = newPaths;

        var newCursors = ArrayPool<int>.Shared.Rent(newSize);
        _cursors.AsSpan(0, _count).CopyTo(newCursors);
        ArrayPool<int>.Shared.Return(_cursors!);
        _cursors = newCursors;

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

        if (_cursors is not null)
        {
            ArrayPool<int>.Shared.Return(_cursors);
            _cursors = null;
        }

        if (_slotIndices is not null)
        {
            ArrayPool<int>.Shared.Return(_slotIndices);
            _slotIndices = null;
        }

        _count = 0;
        _pathCount = 0;
    }
}
