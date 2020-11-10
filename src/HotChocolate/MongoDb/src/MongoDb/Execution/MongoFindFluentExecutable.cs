using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
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
                    new MongoDbFilterDefinition<T>(
                        new AndFilterDefinition(
                            new MongoDbFilterDefinitionWrapper<T>(_findFluent.Filter),
                            Filters));
            }

            if (Sorting is not null)
            {
                if (pipeline.Options?.Sort is {} sortDefinition)
                {
                    pipeline.Sort(
                        new MongoDbSortDefinition<T>(
                            new MongoDbCombinedSortDefinition(
                                new MongoDbSortDefinitionWrapper<T>(sortDefinition),
                                Sorting)));
                }
                else
                {
                    pipeline = pipeline.Sort(new MongoDbSortDefinition<T>(Sorting));
                }
            }

            return pipeline;
        }
    }
}
