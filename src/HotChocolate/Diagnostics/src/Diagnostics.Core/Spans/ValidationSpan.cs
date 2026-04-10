using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ValidationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ValidationSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Document Validation");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new ValidationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.IsOperationDocumentValid())
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichValidateDocument(context, Activity);
    }
}
