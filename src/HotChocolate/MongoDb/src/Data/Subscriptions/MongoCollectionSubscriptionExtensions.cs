using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Subscriptions;

public static class MongoCollectionSubscriptionExtensions
{
    public static async IAsyncEnumerable<T> ObserveChanges<T>(
        this IMongoCollection<T> mongoCollection,
        Func<ChangeStreamDocument<T>, bool> filter,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cursor = await mongoCollection.WatchAsync(cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var streamDocument in cursor.Current.Where(filter))
            {
                yield return streamDocument.FullDocument;
            }
        }
    }
}
