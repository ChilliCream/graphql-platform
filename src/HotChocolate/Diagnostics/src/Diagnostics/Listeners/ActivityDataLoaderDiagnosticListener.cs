using System.Diagnostics;
using GreenDonut;
using HotChocolate.Diagnostics.Scopes;
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

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new DataLoaderBatchScope<TKey>(enricher, dataLoader, keys, activity);
    }

    public override IDisposable RunBatchDispatchCoordinator()
    {
        var activity = Source.StartActivity("BatchCoordinator");

        if (activity is null)
        {
            return EmptyScope;
        }

        return new DataLoaderBatchDispatchCoordinatorScope(enricher, activity);
    }

    public override void BatchDispatchError(System.Exception error)
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
