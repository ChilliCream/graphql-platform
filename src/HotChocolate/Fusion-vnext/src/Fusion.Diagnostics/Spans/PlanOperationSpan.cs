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

        if (context.TryGetDocument(out var document, out _)
            && document.GetOperation(context.Request.OperationName) is { } operation)
        {
            activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operation.Operation]);

            var operationName = operation.Name?.Value;
            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new PlanOperationSpan(activity, context, enricher, operationPlanId);
    }

    protected override void OnComplete()
    {
        if (context.GetOperationPlan() is not null)
        {
            Activity.MarkAsSuccess();
        }

        enricher.EnrichPlanOperation(Activity, context, operationPlanId);
    }
}
