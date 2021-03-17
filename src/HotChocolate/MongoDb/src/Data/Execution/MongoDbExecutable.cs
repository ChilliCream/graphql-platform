using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// Is the base class for a executable for the MongoDb.
    /// </summary>
    public abstract class MongoDbExecutable<T>
        : IExecutable<T>
        , IMongoDbExecutable
    {
        /// <summary>
        /// The filter definition that was set by <see cref="WithFiltering"/>
        /// </summary>
        protected MongoDbFilterDefinition? Filters { get; private set; }

        /// <summary>
        /// The sort definition that was set by <see cref="WithSorting"/>
        /// </summary>
        protected MongoDbSortDefinition? Sorting { get; private set; }

        /// <summary>
        /// The projection definition that was set by <see cref="WithProjection"/>
        /// </summary>
        protected MongoDbProjectionDefinition? Projection { get; private set; }

        /// <inheritdoc />
        public IMongoDbExecutable WithFiltering(MongoDbFilterDefinition filters)
        {
            Filters = filters;
            return this;
        }

        /// <inheritdoc />
        public IMongoDbExecutable WithSorting(MongoDbSortDefinition sorting)
        {
            Sorting = sorting;
            return this;
        }

        /// <inheritdoc />
        public IMongoDbExecutable WithProjection(MongoDbProjectionDefinition projection)
        {
            Projection = projection;
            return this;
        }

        /// <inheritdoc />
        public abstract object Source { get; }

        /// <inheritdoc />
        public abstract ValueTask<IList> ToListAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract string Print();
    }
}
