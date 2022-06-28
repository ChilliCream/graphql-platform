#if NET6_0_OR_GREATER
using System;

namespace HotChocolate.Execution;

internal sealed class IndexerPathSegmentBuffer : PathSegmentBuffer<IndexerPathSegment>
{
    private readonly Path?[] _parents;

    public IndexerPathSegmentBuffer(int capacity) : base(capacity)
    {
        _parents = new Path?[capacity];
    }

    protected override IndexerPathSegment Create(int index)
    {
        return new PooledIndexerPathSegment(index, _parents);
    }

    protected override void Clear(IndexerPathSegment?[] buffer, int index)
    {
        _parents.AsSpan(0, index).Clear();
    }
}
#endif
