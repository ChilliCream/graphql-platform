using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace HotChocolate.MongoDb.Execution
{
    public abstract class MongoExecutable<T>
        : IExecutable<T>
        , IMongoExecutable
    {
        protected FilterDefinition<BsonDocument> Filters { get; private set; } =
            FilterDefinition<BsonDocument>.Empty;

        protected SortDefinition<BsonDocument>? Sorting { get; private set; }


        protected FindOptions<T>? Options { get; private set; }

        public IMongoExecutable WithOptions(FindOptions<T> options)
        {
            Options = options;
            return this;
        }

        public IMongoExecutable WithFiltering(FilterDefinition<BsonDocument> filters)
        {
            Filters = filters;
            return this;
        }

        public IMongoExecutable WithSorting(SortDefinition<BsonDocument> sorting)
        {
            Sorting = sorting;
            return this;
        }

        async ValueTask<object> IExecutable.ExecuteAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken);
        }

        public abstract ValueTask<IReadOnlyList<T>> ExecuteAsync(
            CancellationToken cancellationToken);

        public abstract string Print();
    }
}
