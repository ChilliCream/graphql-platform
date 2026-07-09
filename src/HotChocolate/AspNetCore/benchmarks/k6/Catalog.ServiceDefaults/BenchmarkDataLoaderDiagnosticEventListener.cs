using System.Diagnostics;
using System.Diagnostics.Metrics;
using GreenDonut;

namespace Microsoft.Extensions.Hosting;

public class BenchmarkDataLoaderDiagnosticEventListener : DataLoaderDiagnosticEventListener
{
    private static readonly Meter s_meter = new("GreenDonut.DataLoader", "1.0.0");

    private static readonly Counter<long> s_batchesExecuted =
        s_meter.CreateCounter<long>("dataloader.batches.executed", description: "Number of batches executed");

    private static readonly Counter<long> s_batchesSucceeded =
        s_meter.CreateCounter<long>("dataloader.batches.succeeded", description: "Number of batches that succeeded");

    private static readonly Counter<long> s_batchesFailed =
        s_meter.CreateCounter<long>("dataloader.batches.failed", description: "Number of batches that failed");

    private static readonly Histogram<int> s_batchSize =
        s_meter.CreateHistogram<int>("dataloader.batch.size", description: "Number of items in a batch");

    private static readonly Counter<long> s_cacheHits =
        s_meter.CreateCounter<long>("dataloader.cache.hits", description: "Number of items resolved from cache");

    public override IDisposable ExecuteBatch<TKey>(IDataLoader dataLoader, IReadOnlyList<TKey> keys)
    {
        s_batchesExecuted.Add(1);
        s_batchSize.Record(keys.Count);
        return EmptyScope;
    }

    public override void BatchResults<TKey, TValue>(IReadOnlyList<TKey> keys, ReadOnlySpan<Result<TValue?>> values) where TValue : default
    {
        s_batchesSucceeded.Add(1);
        base.BatchResults(keys, values);
    }

    public override void BatchError<TKey>(IReadOnlyList<TKey> keys, Exception error)
    {
        s_batchesFailed.Add(1);
        Activity.Current?.AddException(error);
        base.BatchError(keys, error);
    }

    public override void ResolvedTaskFromCache(IDataLoader dataLoader, PromiseCacheKey cacheKey, Task task)
    {
        s_cacheHits.Add(1);
        base.ResolvedTaskFromCache(dataLoader, cacheKey, task);
    }
}
