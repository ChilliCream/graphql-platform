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

        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

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
