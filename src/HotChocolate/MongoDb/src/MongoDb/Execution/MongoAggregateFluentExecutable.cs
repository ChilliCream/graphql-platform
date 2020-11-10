using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public class MongoAggregateFluentExecutable<T> : MongoExecutable<T>
    {
        private readonly IAggregateFluent<T> _aggregate;

        public MongoAggregateFluentExecutable(IAggregateFluent<T> aggregate)
        {
            _aggregate = aggregate;
        }

        public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken) =>
            await BuildPipeline()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        public override string Print() => BuildPipeline().ToString() ?? "";

        private IAggregateFluent<T> BuildPipeline()
        {
            IAggregateFluent<T> pipeline = _aggregate;
            if (Sorting is not null)
            {
                pipeline = pipeline.Sort(new MongoDbSortDefinition<T>(Sorting));
            }

            if (Filters is not null)
            {
                pipeline = pipeline.Match(new MongoDbFilterDefinition<T>(Filters));
            }

            return pipeline;
        }
    }
}
