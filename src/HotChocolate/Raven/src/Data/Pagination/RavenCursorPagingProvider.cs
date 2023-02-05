using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

public class RavenCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(RavenCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public override bool CanHandle(IExtendedType source)
        => source.Source.IsGenericType &&
            source.Source.GetGenericTypeDefinition() is { } type && (
                type == typeof(IRavenQueryable<>) ||
                type == typeof(IAsyncDocumentQuery<>) ||
                type == typeof(IDocumentQuery<>));

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
            .Invoke(null, new object[] { options })!;
    }

    private static RavenCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options)
        => new(options);
}
