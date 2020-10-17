using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public class MongoCollectionExecutable<T> : MongoExecutable<T>
    {
        private readonly IMongoCollection<T> _collection;

        public MongoCollectionExecutable(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public override async ValueTask<IReadOnlyList<T>> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            FindOptions<T> options = Options ?? new FindOptions<T>();

            if (Sorting is not null)
            {
                options.Sort = Sorting.DefaultRender();
            }

            IAsyncCursor<T> cursor = await _collection
                .FindAsync(Filters.DefaultRender(), options, cancellationToken)
                .ConfigureAwait(false);

            return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public override string Print()
        {
            var aggregations = new BsonDocument { { "$match", Filters.DefaultRender() } };

            if (Sorting is not null)
            {
                aggregations["$sort"] = Sorting.DefaultRender();
            }

            return aggregations.ToString();
        }
    }
}
