using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class CompileOperationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricher enricher) : SpanBase(activity)
{
    public static CompileOperationSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricher enricher)
    {
        var activity = source.StartActivity("GraphQL Operation Planning");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new CompileOperationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.TryGetOperation(out var operation))
        {
            Activity.SetStatus(ActivityStatusCode.Ok);

            Activity.EnrichOperation(operation.Kind, operation.Name);
        }

        enricher.EnrichCompileOperation(context, Activity);
    }
}
