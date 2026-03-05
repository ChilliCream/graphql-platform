using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class AnalyzeOperationComplexitySpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    private bool _costSet;

    public static AnalyzeOperationComplexitySpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Complexity Analyzation");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

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

        return new AnalyzeOperationComplexitySpan(activity, context, enricher);
    }

    public void SetCost(double fieldCost, double typeCost)
    {
        Activity.SetTag(GraphQL.Operation.FieldCost, fieldCost);
        Activity.SetTag(GraphQL.Operation.TypeCost, typeCost);

        _costSet = true;

        enricher.EnrichOperationCost(Activity, context, fieldCost, typeCost);
    }

    protected override void OnComplete()
    {
        if (_costSet)
        {
            Activity.MarkAsSuccess();
        }

        enricher.EnrichAnalyzeOperationCost(Activity, context);
    }
}
