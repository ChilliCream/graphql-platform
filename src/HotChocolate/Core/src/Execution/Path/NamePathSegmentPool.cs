using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class NamePathSegmentPool : DefaultObjectPool<PathSegmentBuffer<NamePathSegment>>
{
    public NamePathSegmentPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<PathSegmentBuffer<NamePathSegment>>
    {
        private static readonly NamePathSegmentPolicy _policy = new();

        public override PathSegmentBuffer<NamePathSegment> Create() => new(256, _policy);

        public override bool Return(PathSegmentBuffer<NamePathSegment> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class NamePathSegmentPolicy : PooledObjectPolicy<NamePathSegment>
    {
        private readonly string _default = "default";

        public override NamePathSegment Create() => new();

        public override bool Return(NamePathSegment segment)
        {
            segment.Name = _default;
            segment.Parent = Path.Root;
            return true;
        }
    }
}
