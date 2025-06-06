using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Pagination;

internal sealed class EfQueryableCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo s_createHandler =
        typeof(EfQueryableCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public override bool CanHandle(IExtendedType source)
        => source.IsArrayOrList;

    protected override CursorPagingHandler CreateHandler(
        IExtendedType source,
        PagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (CursorPagingHandler)s_createHandler
            .MakeGenericMethod(source.ElementType?.Source ?? source.Source)
            .Invoke(null, [options])!;
    }

    private static EfQueryableCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options)
        => new(options);
}
