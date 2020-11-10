using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.MongoDb.Data;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

        public override async ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
        {
            IBsonSerializerRegistry serializers = _collection.Settings.SerializerRegistry;
            IBsonSerializer bsonSerializer = _collection.DocumentSerializer;

            FindOptions<T> options = Options as FindOptions<T> ?? new FindOptions<T>();
            BsonDocument filters = new BsonDocument();

            if (Sorting is not null)
            {
                options.Sort = Sorting.Render(bsonSerializer, serializers);
            }

            if (Filters is not null)
            {
                filters = Filters.Render(bsonSerializer, serializers);
            }

            IAsyncCursor<T> cursor = await _collection
                .FindAsync(filters, options, cancellationToken)
                .ConfigureAwait(false);

            return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask<object?> FirstOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        public override async ValueTask<object?> SingleOrDefaultAsync(
            CancellationToken cancellationToken) =>
            await BuildPipeline()
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        public override string Print() => BuildPipeline().ToString() ?? "";

        public IFindFluent<T, T> BuildPipeline()
        {
            FindOptions options = Options as FindOptions ?? new FindOptions();
            FilterDefinition<T> filters = FilterDefinition<T>.Empty;

            if (Filters is not null)
            {
                filters = new MongoDbFilterDefinition<T>(Filters);
            }

            IFindFluent<T, T> pipeline = _collection.Find(filters, options);

            if (Sorting is not null)
            {
                pipeline = pipeline.Sort(new MongoDbSortDefinition<T>(Sorting));
            }

            return pipeline;
        }
    }

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
