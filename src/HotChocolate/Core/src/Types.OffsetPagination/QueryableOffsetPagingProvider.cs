using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Represents the default paging provider for in-memory collections or queryables.
/// </summary>
public class QueryableOffsetPagingProvider
    : OffsetPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(QueryableOffsetPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public override bool CanHandle(IExtendedType source)
    {
        throw new NotImplementedException();
    }

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
            .Invoke(null, [options,])!;
    }

    private static QueryableOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) =>
        new QueryableOffsetPagingHandler<TEntity>(options);
}
