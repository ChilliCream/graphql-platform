namespace GreenDonut.Selectors;

internal sealed class SelectionDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    , ISelectionDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly DataLoaderBase<TKey, TValue> _root;

    public SelectionDataLoader(
        DataLoaderBase<TKey, TValue> root,
        string selectionKey)
        : base(root.BatchScheduler, root.Options)
    {
        _root = root;
        CacheKeyType = $"{root.CacheKeyType}:{selectionKey}";
        ContextData = root.ContextData;
    }

    public IDataLoader<TKey, TValue> Root => _root;

    protected internal override string CacheKeyType { get; }

    protected override bool AllowCachePropagation => false;

    protected override bool AllowBranching => true;

    protected internal override ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
        => _root.FetchAsync(keys, results, context, cancellationToken);
}
