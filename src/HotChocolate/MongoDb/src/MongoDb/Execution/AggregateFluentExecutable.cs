using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.MongoDb.Execution
{

    internal class AggregateFluentExecutable<T> : IAggregateFluentExecutable<T>
    {
        private readonly IAggregateFluent<T> _aggregateFluent;

        public AggregateFluentExecutable(IAggregateFluent<T> aggregateFluent)
        {
            this._aggregateFluent = aggregateFluent ?? throw new ArgumentNullException(nameof(aggregateFluent));
        }

        public IMongoDatabase Database => _aggregateFluent.Database;

        public AggregateOptions Options => _aggregateFluent.Options;

        public IList<IPipelineStageDefinition> Stages => _aggregateFluent.Stages;

        public IAggregateFluent<AggregateCountResult> Count()
        {
            return _aggregateFluent.Count();
        }

        public IAggregateFluentExecutable<T> Limit(int limit)
        {
            return _aggregateFluent.Limit(limit).AsExecutable();
        }

        public IAggregateFluentExecutable<T> Match(FilterDefinition<T> filter)
        {
            return _aggregateFluent.Match(filter).AsExecutable();
        }

        public IAggregateFluentExecutable<T> Skip(int skip)
        {
            return _aggregateFluent.Skip(skip).AsExecutable();
        }

        public IAggregateFluentExecutable<T> Sort(SortDefinition<T> sort)
        {
            return _aggregateFluent.Sort(sort).AsExecutable();
        }

        public IAsyncCursor<T> ToCursor(CancellationToken cancellationToken = default)
        {
            return _aggregateFluent.ToCursor(cancellationToken);
        }

        public Task<IAsyncCursor<T>> ToCursorAsync(CancellationToken cancellationToken = default)
        {
            return _aggregateFluent.ToCursorAsync(cancellationToken);
        }

        public IAggregateFluentExecutable<T> UnionWith<TWith>(IMongoCollection<TWith> withCollection, PipelineDefinition<TWith, T> withPipeline = null)
        {
            return _aggregateFluent.UnionWith(withCollection, withPipeline).AsExecutable();
        }

        public string Print() => _aggregateFluent.ToString() ?? string.Empty;

        async ValueTask<object> IExecutable.ExecuteAsync(CancellationToken cancellationToken)
        {
            IAsyncCursor<T>? cursor = await ToCursorAsync().ConfigureAwait(false);
            return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
