using System.Diagnostics;
using GreenDonut;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityDataLoaderDiagnosticListener(
    ActivityEnricher enricher,
    InstrumentationOptions options)
    : DataLoaderDiagnosticEventListener
{
    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        if (options.SkipDataLoaderBatch)
        {
            return EmptyScope;
        }

        var span = DataLoaderBatchSpan<TKey>.Start(Source, dataLoader, keys);

        if (span is null)
        {
            return EmptyScope;
        }

        if (options.IncludeDataLoaderKeys)
        {
            var temp = keys.Select(t => t.ToString()).ToArray();
            span.Activity.SetTag(SemanticConventions.GraphQL.DataLoader.Batch.Keys, temp);
        }

        return span;
    }

    public override IDisposable RunBatchDispatchCoordinator()
    {
        var span = DataLoaderDispatchSpan.Start(Source);

        return span ?? EmptyScope;
    }

    public override void BatchDispatchError(Exception error)
    {
#if NET9_0_OR_GREATER
        Activity.Current?.AddException(error);
#else
        Activity.Current?.SetStatus(ActivityStatusCode.Error, error.Message);
#endif
    }

    public override void BatchEvaluated(int openBatches)
    {
        Activity.Current?.AddEvent(new ActivityEvent(
            "BatchEvaluated",
            tags: new ActivityTagsCollection
            {
                { "openBatches", openBatches }
            }));
    }

    public override void BatchDispatched(int dispatchedBatches)
    {
        Activity.Current?.AddEvent(new ActivityEvent(
            "BatchDispatched",
            tags: new ActivityTagsCollection
            {
                { "dispatchedBatches", dispatchedBatches }
            }));
    }
}
