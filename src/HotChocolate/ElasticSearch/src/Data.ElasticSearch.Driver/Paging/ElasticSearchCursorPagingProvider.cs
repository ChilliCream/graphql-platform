using System.Reflection;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler;

    static ElasticSearchCursorPagingProvider()
    {
        _createHandler =
            typeof(ElasticSearchCursorPagingProvider).GetMethod(
                nameof(CreateHandlerInternal),
                BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
    {
        return typeof(IElasticSearchExecutable).IsAssignableFrom(source.Source) ||
               source.Source.IsGenericType &&
               source.Source.GetGenericTypeDefinition() is { } type && (
                   type == typeof(IElasticSearchExecutable<>));
    }

    /// <inheritdoc />
    protected override CursorPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return (CursorPagingHandler)_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, new object[] { options })!;
    }

    private static ElasticSearchCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) => new(options);
}
