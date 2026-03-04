using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class AnalyzeOperationComplexitySpan(Activity activity) : SpanBase(activity)
{
    private bool _costSet;

    public static AnalyzeOperationComplexitySpan? Start(ActivitySource source, RequestContext context)
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
            activity.SetTag(GraphQL.Operation.Type, operation.Operation);
        }

        var operationName = context.Request.OperationName;
        // TODO: This should be conditional
        if (!string.IsNullOrEmpty(operationName))
        {
            activity.SetTag(GraphQL.Operation.Name, operationName);
        }

        var documentInfo = context.OperationDocumentInfo;
        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        // TODO: We need a good mechanism to determine if persisted operations are enabled
        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new AnalyzeOperationComplexitySpan(activity);
    }

    public void SetCost(double fieldCost, double typeCost)
    {
        Activity.SetTag(GraphQL.Operation.FieldCost, fieldCost);
        Activity.SetTag(GraphQL.Operation.TypeCost, typeCost);

        _costSet = true;
    }

    protected override void OnComplete()
    {
        if (_costSet)
        {
            Activity.MarkAsSuccess();
        }
    }
}
