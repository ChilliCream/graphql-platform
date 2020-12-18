using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    /// <summary>
    /// A executable that is based on <see cref="IAggregateFluent{TInput}"/>
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class MongoDbAggregateFluentExecutable<T> : MongoDbExecutable<T>
    {
        private readonly IAggregateFluent<T> _aggregate;

        public MongoDbAggregateFluentExecutable(IAggregateFluent<T> aggregate)
        {
            _aggregate = aggregate;
        }

        /// <inheritdoc />
        public override object Source => _aggregate;

        /// <inheritdoc />
        public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken) =>
            await BuildPipeline()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public override string Print() => BuildPipeline().ToString() ?? "";

        /// <summary>
        /// Applies filtering sorting and projections on the <see cref="IExecutable{T}.Source"/>
        /// </summary>
        /// <returns>A aggregate fluent including the configuration of this executable</returns>
        public IAggregateFluent<T> BuildPipeline()
        {
            IAggregateFluent<T> pipeline = _aggregate;
            if (Sorting is not null)
            {
                pipeline = pipeline.Sort(Sorting.ToSortDefinition<T>());
            }

            if (Filters is not null)
            {
                pipeline = pipeline.Match(Filters.ToFilterDefinition<T>());
            }

            if (Projection is not null)
            {
                pipeline = pipeline.Project<T>(Projection.ToProjectionDefinition<T>());
            }

            return pipeline;
        }
    }
}
