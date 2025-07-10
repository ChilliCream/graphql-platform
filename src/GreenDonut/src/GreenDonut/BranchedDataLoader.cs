namespace GreenDonut;

/// <summary>
/// This class represents a branched <see cref="IDataLoader{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
/// <typeparam name="TValue">
/// The type of the value.
/// </typeparam>
public class BranchedDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    where TKey : notnull
{
    private readonly DataLoaderBase<TKey, TValue> _root;

    public BranchedDataLoader(
        DataLoaderBase<TKey, TValue> root,
        string key)
        : base(root.BatchScheduler, root.Options)
    {
        _root = root;
        CacheKeyType = $"{root.CacheKeyType}:{key}";
        ContextData = root.ContextData;
    }

    public IDataLoader<TKey, TValue> Root => _root;

    protected internal override string CacheKeyType { get; }

    protected sealed override bool AllowCachePropagation => false;

    protected override bool AllowBranching => true;

    protected internal override ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
        => _root.FetchAsync(keys, results, context, cancellationToken);
}
