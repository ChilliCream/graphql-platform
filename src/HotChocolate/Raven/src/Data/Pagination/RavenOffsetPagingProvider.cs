using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

/// <summary>
/// An offset paging provider for Raven that create pagination queries
/// </summary>
public sealed class RavenOffsetPagingProvider : OffsetPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(RavenOffsetPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
        => source.Source.IsGenericType &&
            source.Source.GetGenericTypeDefinition() is { } type && (
                type == typeof(IRavenQueryable<>) ||
                type == typeof(IAsyncDocumentQuery<>));

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
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source.GetGenericArguments()[0])
            .Invoke(null, [options,])!;
    }

    private static RavenOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) => new(options);
}
