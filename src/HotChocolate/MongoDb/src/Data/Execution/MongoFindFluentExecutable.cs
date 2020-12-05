using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.MongoDb;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Execution
{
    public class MongoFindFluentExecutable<T> : MongoExecutable<T>
    {
        private readonly IFindFluent<T, T> _findFluent;

        public MongoFindFluentExecutable(IFindFluent<T, T> findFluent)
        {
            _findFluent = findFluent;
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

            if (Projections is not null)
            {
                pipeline = pipeline.Project<T>(Projections.ToProjectionDefinition<T>());
            }

            return pipeline;
        }
    }
}
