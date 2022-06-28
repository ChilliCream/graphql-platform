using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class NamePathSegmentPool
    : DefaultObjectPool<PathSegmentBuffer<NamePathSegment>>
{
    public NamePathSegmentPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

#if NET6_0_OR_GREATER
    private sealed class BufferPolicy
        : IPooledObjectPolicy<PathSegmentBuffer<NamePathSegment>>
    {
        public PathSegmentBuffer<NamePathSegment> Create()
            => new NamePathSegmentBuffer(256);

        public bool Return(PathSegmentBuffer<NamePathSegment> obj)
        {
            obj.Reset();
            return true;
        }
    }
#else
    private sealed class BufferPolicy
        : IPooledObjectPolicy<PathSegmentBuffer<NamePathSegment>>
    {
        private static readonly NamePathSegmentPolicy _policy = new();

        public PathSegmentBuffer<NamePathSegment> Create() =>
            new ObjectPoolPathSegmentBuffer<NamePathSegment>(256, _policy);

        public bool Return(PathSegmentBuffer<NamePathSegment> obj)
        {
            obj.Reset();
            return true;
        }

        private sealed class NamePathSegmentPolicy : IPooledObjectPolicy<NamePathSegment>
        {
            private readonly NameString _default = new("default");

            public NamePathSegment Create() => new();

            public bool Return(NamePathSegment segment)
            {
                segment.Name = _default;
                segment.Parent = Path.Root;
                return true;
            }
        }
    }
#endif
}
