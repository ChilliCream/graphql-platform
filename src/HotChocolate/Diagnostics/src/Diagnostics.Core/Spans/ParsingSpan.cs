using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics;

internal sealed class ParsingSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ParsingSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Document Parsing");

        if (activity is null)
        {
            return null;
        }

        // We do not set this here, as parsing can happen in the HTTP middleware
        // or the HotChocolate pipeline.
        // For the moment we just track both as regular spans.
        // Maybe in the future we can reconcile this.
        // activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        return new ParsingSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.TryGetOperationDocument(out _, out _))
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        Activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        enricher.EnrichParseDocument(context, Activity);
    }
}
