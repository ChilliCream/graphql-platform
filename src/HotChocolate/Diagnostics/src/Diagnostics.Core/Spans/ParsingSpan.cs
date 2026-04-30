using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

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

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

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
