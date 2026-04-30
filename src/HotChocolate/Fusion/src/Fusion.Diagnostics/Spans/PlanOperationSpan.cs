using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class PlanOperationSpan(
    Activity activity,
    RequestContext context,
    FusionActivityEnricher enricher,
    string operationPlanId) : SpanBase(activity)
{
    public static PlanOperationSpan? Start(
        ActivitySource source,
        RequestContext context,
        FusionActivityEnricher enricher,
        string operationPlanId)
    {
        var activity = source.StartActivity("GraphQL Operation Planning");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        // Only the document hash is set here. The document.id already lives on the
        // parent request span via EnrichDocumentInfo and would be redundant on the
        // planning span.
        var hash = context.OperationDocumentInfo.Hash;
        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        return new PlanOperationSpan(activity, context, enricher, operationPlanId);
    }

    protected override void OnComplete()
    {
        if (context.GetOperationPlan() is { } plan)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);

            var operation = plan.Operation;
            Activity.EnrichOperation(operation.Definition.Operation, operation.Name);
        }

        enricher.EnrichPlanOperation(context, operationPlanId, Activity);
    }
}
