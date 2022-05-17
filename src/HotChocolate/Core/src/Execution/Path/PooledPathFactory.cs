using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

internal sealed class PooledPathFactory
    : PathFactory
{
    private readonly PathSegmentFactory<IndexerPathSegment> _indexerPathFactory;
    private readonly PathSegmentFactory<NamePathSegment> _namePathFactory;

    public PooledPathFactory(
        ObjectPool<PathSegmentBuffer<IndexerPathSegment>> indexerPathPool,
        ObjectPool<PathSegmentBuffer<NamePathSegment>> namePathPool)
    {
        _indexerPathFactory = new PathSegmentFactory<IndexerPathSegment>(indexerPathPool);
        _namePathFactory = new PathSegmentFactory<NamePathSegment>(namePathPool);
    }

    public void Clear()
    {
        _indexerPathFactory.Clear();
        _namePathFactory.Clear();
    }

    protected override IndexerPathSegment CreateIndexer() => _indexerPathFactory.Get();

    protected override NamePathSegment CreateNamed() => _namePathFactory.Get();
}
