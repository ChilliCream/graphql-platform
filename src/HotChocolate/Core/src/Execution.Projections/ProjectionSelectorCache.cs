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

    internal SelectorExpression<TValue> GetOrCreate<TValue>(
        Selection selection,
        ulong includeFlags,
        Func<Selection, ulong, SelectorExpression<TValue>> create)
    {
        var key = new SelectorCacheKey(
            selection.DeclaringOperation.CacheId,
            selection.Id,
            includeFlags,
            typeof(TValue));

        return (SelectorExpression<TValue>)_cache.GetOrCreate(
            key,
            static (_, state) => state.Create(state.Selection, state.IncludeFlags),
            new SelectorCacheCreateState<TValue>(selection, includeFlags, create));
    }

    private readonly record struct SelectorCacheKey(
        long OperationId,
        int SelectionId,
        ulong IncludeFlags,
        Type ValueType);

    private readonly record struct SelectorCacheCreateState<TValue>(
        Selection Selection,
        ulong IncludeFlags,
        Func<Selection, ulong, SelectorExpression<TValue>> Create);
}
