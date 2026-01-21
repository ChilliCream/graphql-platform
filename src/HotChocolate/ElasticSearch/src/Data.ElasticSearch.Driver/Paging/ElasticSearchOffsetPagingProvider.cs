using System.Reflection;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Data.ElasticSearch.Paging;

public class ElasticSearchOffsetPagingProvider : OffsetPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(ElasticSearchOffsetPagingProvider)
            .GetMethod(nameof(CreateHandlerInternal), Static | NonPublic)!;

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
    {
        return typeof(IElasticSearchExecutable).IsAssignableFrom(source.Source) ||
            source.Source.IsGenericType &&
            source.Source.GetGenericTypeDefinition() is { } type &&
            type == typeof(IElasticSearchExecutable<>);
    }

    /// <inheritdoc />
    protected override OffsetPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return (OffsetPagingHandler)_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, new object[] { options })!;
    }

    private static ElasticSearchOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) => new(options);
}
