using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public abstract class MongoExecutable<T>
        : IExecutable<T>
        , IMongoExecutable
    {
        protected MongoDbFilterDefinition? Filters { get; private set; }

        protected MongoDbSortDefinition? Sorting { get; private set; }

        protected FindOptions<T>? Options { get; private set; }

        public IMongoExecutable WithOptions(FindOptions<T> options)
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

        async ValueTask<object> IExecutable.ExecuteAsync(CancellationToken cancellationToken)
        {
            return await ExecuteAsync(cancellationToken);
        }

        public abstract ValueTask<IReadOnlyList<T>> ExecuteAsync(
            CancellationToken cancellationToken);

        public abstract string Print();
    }
}
