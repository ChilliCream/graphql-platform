using GreenDonut;
using GreenDonut.Internals;

namespace HotChocolate.Pagination;

internal sealed class PagingDataLoader<TKey, TValue>
    : DataLoaderBase<TKey, TValue>
    , IPagingDataLoader<TKey, TValue>
    where TKey : notnull
{
    private readonly DataLoaderBase<TKey, TValue> _root;

    public PagingDataLoader(
        DataLoaderBase<TKey, TValue> root,
        string pagingKey)
        : base(DataLoaderHelper.GetBatchScheduler(root), DataLoaderHelper.GetOptions(root))
    {
        _root = root;
        CacheKeyType = $"{DataLoaderHelper.GetCacheKeyType(root)}:{pagingKey}";
    }

    public IDataLoader<TKey, TValue> Root => _root;

    public PagingArguments PagingArguments
        => (PagingArguments)ContextData[typeof(PagingArguments).FullName!]!;

    protected internal override string CacheKeyType { get; }

    protected override bool AllowCachePropagation => false;

    protected override bool AllowBranching => false;

    protected internal override ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue?>> results,
        DataLoaderFetchContext<TValue> context,
        CancellationToken cancellationToken)
        => DataLoaderHelper.FetchAsync(_root, keys, results, context, cancellationToken);
}
