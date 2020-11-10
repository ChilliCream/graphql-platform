using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public abstract class MongoExecutable<T>
        : IExecutable<T>
        , IMongoExecutable
    {
        protected MongoDbFilterDefinition? Filters { get; private set; }

        protected MongoDbSortDefinition? Sorting { get; private set; }

        protected FindOptionsBase? Options { get; private set; }

        public IMongoExecutable WithOptions(FindOptionsBase options)
        {
            Options = options;
            return this;
        }

        public IMongoExecutable WithFiltering(MongoDbFilterDefinition filters)
        {
            Filters = filters;
            return this;
        }

        public IMongoExecutable WithSorting(MongoDbSortDefinition sorting)
        {
            Sorting = sorting;
            return this;
        }

        public object Source { get; }

        public abstract ValueTask<IList> ToListAsync(CancellationToken cancellationToken);

        public abstract ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);

        public abstract ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken);

        public abstract string Print();
    }
}
