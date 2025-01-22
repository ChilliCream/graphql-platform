using System.Collections.Concurrent;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The cursor pagination provider for <see cref="IQueryable{T}"/>.
/// </summary>
public class QueryableCursorPagingProvider : CursorPagingProvider
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _factoryCache = new();
    private readonly bool? _inlineTotalCount;

    private static readonly MethodInfo _createHandler =
        typeof(QueryableCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public QueryableCursorPagingProvider() { }

    public QueryableCursorPagingProvider(bool? inlineTotalCount)
    {
        _inlineTotalCount = inlineTotalCount;
    }

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.IsArrayOrList;
    }

    /// <inheritdoc />
    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var key = source.ElementType?.Source ?? source.Source;
        var factory = _factoryCache.GetOrAdd(key, static type => _createHandler.MakeGenericMethod(type));
        return (CursorPagingHandler)factory.Invoke(null, [options, _inlineTotalCount])!;
    }

    private static QueryableCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options, bool? inlineTotalCount)
        => new(options, inlineTotalCount);
}
