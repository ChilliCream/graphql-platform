using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class NamePathSegmentPool
    : DefaultObjectPool<PathSegmentBuffer<NamePathSegment>>
{
    public NamePathSegmentPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

    private sealed class BufferPolicy
        : IPooledObjectPolicy<PathSegmentBuffer<NamePathSegment>>
    {
        private static readonly NamePathSegmentPolicy _policy = new();

        public PathSegmentBuffer<NamePathSegment> Create() => new(256, _policy);

        public bool Return(PathSegmentBuffer<NamePathSegment> obj)
        {
            obj.Reset();
            return true;
        }
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
