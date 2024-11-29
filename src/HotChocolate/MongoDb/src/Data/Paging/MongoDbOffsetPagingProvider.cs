using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Paging;

/// <summary>
/// An offset paging provider for MongoDb that create pagination queries
/// </summary>
public class MongoDbOffsetPagingProvider : OffsetPagingProvider
{
    private static readonly MethodInfo _createHandler =
        typeof(MongoDbOffsetPagingProvider).GetMethod(
            nameof(CreateHandlerInternal),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public override bool CanHandle(IExtendedType source)
    {
        return typeof(IMongoDbExecutable).IsAssignableFrom(source.Source) ||
            source.Source.IsGenericType &&
            source.Source.GetGenericTypeDefinition() is { } type && (
                type == typeof(IAggregateFluent<>) ||
                type == typeof(IFindFluent<,>) ||
                type == typeof(IMongoCollection<>));
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

    private static MongoDbOffsetPagingHandler<TEntity> CreateHandlerInternal<TEntity>(
        PagingOptions options) => new(options);
}
