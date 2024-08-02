using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// The cursor pagination provider for <see cref="IQueryable{T}"/>.
/// </summary>
public class QueryableCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(QueryableCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

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

        return (CursorPagingHandler)_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, [options,])!;
    }

    private static QueryableCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) =>
        new(options);
}
