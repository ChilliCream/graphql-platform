using System;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

public class MongoDbCursorPagingProvider : CursorPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(MongoDbCursorPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public override bool CanHandle(IExtendedType source)
        => typeof(IMongoDbExecutable).IsAssignableFrom(source.Source) ||
               source.Source.IsGenericType &&
               source.Source.GetGenericTypeDefinition() is { } type && (
                   type == typeof(IAggregateFluent<>) ||
                   type == typeof(IFindFluent<,>) ||
                   type == typeof(IMongoCollection<>));

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

    private static MongoDbCursorPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options)
        => new(options);
}
