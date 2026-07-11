using System.Diagnostics;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class DataLoaderDispatchSpan(
    Activity activity,
    ActivityEnricher enricher) : SpanBase(activity)
{
    public static DataLoaderDispatchSpan? Start(
        ActivitySource source,
        ActivityEnricher enricher)
    {
        var activity = source.StartActivity("GraphQL DataLoader Dispatch");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.DataLoaderDispatch);

        return new DataLoaderDispatchSpan(activity, enricher);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichRunBatchDispatchCoordinator(Activity);
    }
}
