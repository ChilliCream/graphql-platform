using System.Diagnostics;
using GreenDonut;
using HotChocolate.Diagnostics.Scopes;
using static HotChocolate.Diagnostics.HotChocolateActivitySource;

namespace HotChocolate.Diagnostics.Listeners;

internal sealed class ActivityDataLoaderDiagnosticListener : DataLoaderDiagnosticEventListener
{
    private readonly InstrumentationOptions _options;
    private readonly ActivityEnricher _enricher;

    public ActivityDataLoaderDiagnosticListener(
        ActivityEnricher enricher,
        InstrumentationOptions options)
    {
        _enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        if (_options.SkipDataLoaderBatch)
        {
            return EmptyScope;
        }

        var activity = Source.StartActivity();

        if (activity is null)
        {
            return EmptyScope;
        }

        return new DataLoaderBatchScope<TKey>(_enricher, dataLoader, keys, activity);
    }

    public override IDisposable RunBatchDispatchCoordinator()
    {
        var activity = Source.StartActivity("BatchCoordinator");
        activity?.DisplayName = "Coordinate DataLoader Batches";
        return activity ?? EmptyScope;
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
