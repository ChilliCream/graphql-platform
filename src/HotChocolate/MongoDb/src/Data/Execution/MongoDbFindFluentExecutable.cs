using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Execution
{
    /// <summary>
    /// A executable that is based on <see cref="IFindFluent{TInput,TResult}"/>
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class MongoDbFindFluentExecutable<T> : MongoDbExecutable<T>
    {
        private readonly IFindFluent<T, T> _findFluent;

        public MongoDbFindFluentExecutable(IFindFluent<T, T> findFluent)
        {
            _findFluent = findFluent;
        }

        /// <inheritdoc />
        public override object Source => _findFluent;

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
        /// <returns>A find fluent including the configuration of this executable</returns>
        public IFindFluent<T, T> BuildPipeline()
        {
            IFindFluent<T, T> pipeline = _findFluent;

            if (Filters is not null)
            {
                pipeline.Filter =
                    new AndFilterDefinition(_findFluent.Filter.Wrap(), Filters)
                        .ToFilterDefinition<T>();
            }

            if (Sorting is not null)
            {
                if (pipeline.Options?.Sort is {} sortDefinition)
                {
                    pipeline.Sort(
                        new MongoDbCombinedSortDefinition(sortDefinition.Wrap(), Sorting)
                            .ToSortDefinition<T>());
                }
                else
                {
                    pipeline = pipeline.Sort(Sorting.ToSortDefinition<T>());
                }
            }

            if (Projection is not null)
            {
                pipeline = pipeline.Project<T>(Projection.ToProjectionDefinition<T>());
            }

            return pipeline;
        }
    }
}
