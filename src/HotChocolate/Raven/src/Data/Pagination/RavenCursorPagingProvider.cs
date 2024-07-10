using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

/// <summary>
/// An cursor paging provider for Raven that create pagination queries
/// </summary>
public sealed class RavenCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(RavenCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <inheritdoc />
    public override bool CanHandle(IExtendedType source)
        => source.Source.IsGenericType &&
            source.Source.GetGenericTypeDefinition() is { } type && (
                type == typeof(IRavenQueryable<>) ||
                type == typeof(IAsyncDocumentQuery<>));

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
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source.GetGenericArguments()[0])
            .Invoke(null, [options,])!;
    }

    private static RavenCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options)
        => new(options);
}
