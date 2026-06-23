using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Projections;

internal sealed class ProjectionSelectorCache
{
    public const int DefaultCapacity = 4096;

    private readonly Cache<SelectorCacheKey, SelectorExpression> _cache;

    public ProjectionSelectorCache(
        int capacity = DefaultCapacity,
        CacheDiagnostics? diagnostics = null)
    {
        _cache = new Cache<SelectorCacheKey, SelectorExpression>(capacity, diagnostics);
    }

    public SelectorExpression GetOrCreate<TEntity>(
        Selection selection,
        ulong includeFlags,
        Func<Selection, ulong, SelectorExpression> create)
    {
        var key = new SelectorCacheKey(
            selection.DeclaringOperation.CacheId,
            selection.Id,
            includeFlags,
            typeof(TEntity));

        if (_cache.TryGet(key, out var selectorExpression))
        {
            return selectorExpression;
        }

        return _cache.GetOrCreate(
            key,
            (_, state) => create(state.selection, state.includeFlags),
            (selection, includeFlags));
    }

    private readonly record struct SelectorCacheKey(
        long OperationId,
        int SelectionId,
        ulong IncludeFlags,
        Type ValueType);
}
