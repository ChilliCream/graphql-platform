using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class IndexerPathSegmentPool
    : DefaultObjectPool<PathSegmentBuffer<IndexerPathSegment>>
{
    public IndexerPathSegmentPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<PathSegmentBuffer<IndexerPathSegment>>
    {
        private static readonly IndexerPathSegmentPolicy _policy = new();

        public override PathSegmentBuffer<IndexerPathSegment> Create()
            => new(256, _policy);

        public override bool Return(PathSegmentBuffer<IndexerPathSegment> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class IndexerPathSegmentPolicy : PooledObjectPolicy<IndexerPathSegment>
    {
        public override IndexerPathSegment Create() => new();

        public override bool Return(IndexerPathSegment segment)
        {
            segment.Parent = Path.Root;
            return true;
        }
    }
}
