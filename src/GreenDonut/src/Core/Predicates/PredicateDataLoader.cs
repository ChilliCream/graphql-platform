namespace GreenDonut.Predicates;

internal sealed class PredicateDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    , IPredicateDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly DataLoaderBase<TKey, TValue> _root;

    public PredicateDataLoader(
        DataLoaderBase<TKey, TValue> root,
        string predicateKey)
        : base(root.BatchScheduler, root.Options)
    {
        _root = root;
        CacheKeyType = $"{root.CacheKeyType}:{predicateKey}";
        ContextData = root.ContextData;
    }

    public IDataLoader<TKey, TValue> Root => _root;

    protected internal override string CacheKeyType { get; }

    protected override bool AllowCachePropagation => false;

    protected override bool AllowBranching => false;

    protected internal override ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
        => _root.FetchAsync(keys, results, context, cancellationToken);
}
