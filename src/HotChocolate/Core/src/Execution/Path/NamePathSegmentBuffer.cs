#if NET6_0_OR_GREATER
using System;

namespace HotChocolate.Execution;

internal sealed class NamePathSegmentBuffer : PathSegmentBuffer<NamePathSegment>
{
    private readonly NameString[] _names;
    private readonly Path?[] _parents;

    public NamePathSegmentBuffer(int capacity) : base(capacity)
    {
        _names = new NameString[capacity];
        _parents = new Path?[capacity];
    }

    protected override NamePathSegment Create(int index)
    {
        return new PooledNamePathSegment(index, _names, _parents);
    }

    protected override void Clear(NamePathSegment?[] buffer, int index)
    {
        _names.AsSpan(0, index).Clear();
        _parents.AsSpan(0, index).Clear();
    }
}
#endif
