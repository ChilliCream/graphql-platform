using HotChocolate;
using MongoDB.Driver;
using System.Collections.Generic;

namespace HotChocolate.MongoDb.Execution
{
    public interface IAggregateFluentExecutable<T>: IAsyncCursorSource<T>, IExecutable<T>
    {
        IMongoDatabase Database { get; }
        AggregateOptions Options { get; }
        IList<IPipelineStageDefinition> Stages { get; }

        IAggregateFluent<AggregateCountResult> Count();
        IAggregateFluentExecutable<T> Limit(int limit);
        IAggregateFluentExecutable<T> Match(FilterDefinition<T> filter);
        IAggregateFluentExecutable<T> Skip(int skip);
        IAggregateFluentExecutable<T> Sort(SortDefinition<T> sort);
        IAggregateFluentExecutable<T> UnionWith<TWith>(IMongoCollection<TWith> withCollection, PipelineDefinition<TWith, T> withPipeline = null);
    }
}
