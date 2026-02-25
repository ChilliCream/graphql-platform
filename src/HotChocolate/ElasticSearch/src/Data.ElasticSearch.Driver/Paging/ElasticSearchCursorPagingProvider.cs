using System.Reflection;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(ElasticSearchCursorPagingProvider)
            .GetMethod(nameof(CreateHandlerInternal), Static | NonPublic)!;

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
    {
        return typeof(IElasticSearchExecutable).IsAssignableFrom(source.Source)
            || source.Source.IsGenericType
            && source.Source.GetGenericTypeDefinition() is { } type
            && type == typeof(IElasticSearchExecutable<>);
    }

    /// <inheritdoc />
    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (CursorPagingHandler)_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, [options])!;
    }

    private static ElasticSearchCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) => new(options);
}
