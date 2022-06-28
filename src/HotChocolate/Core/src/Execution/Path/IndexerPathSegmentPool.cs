using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class IndexerPathSegmentPool
    : DefaultObjectPool<PathSegmentBuffer<IndexerPathSegment>>
{
    public IndexerPathSegmentPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

#if NET6_0_OR_GREATER
    private sealed class BufferPolicy
        : IPooledObjectPolicy<PathSegmentBuffer<IndexerPathSegment>>
    {
        public PathSegmentBuffer<IndexerPathSegment> Create()
            => new IndexerPathSegmentBuffer(256);

        public bool Return(PathSegmentBuffer<IndexerPathSegment> obj)
        {
            obj.Reset();
            return true;
        }
    }
#else
    private sealed class BufferPolicy
        : IPooledObjectPolicy<PathSegmentBuffer<IndexerPathSegment>>
    {
        private static readonly IndexerPathSegmentPolicy _policy = new();

        public PathSegmentBuffer<IndexerPathSegment> Create()
            => new ObjectPoolPathSegmentBuffer<IndexerPathSegment>(256, _policy);

        public bool Return(PathSegmentBuffer<IndexerPathSegment> obj)
        {
            obj.Reset();
            return true;
        }

        private sealed class IndexerPathSegmentPolicy : IPooledObjectPolicy<IndexerPathSegment>
        {
            public IndexerPathSegment Create() => new();

            public bool Return(IndexerPathSegment segment)
            {
                segment.Parent = Path.Root;
                return true;
            }
        }
    }
#endif
}
