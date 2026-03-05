using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class VariableCoercionSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static VariableCoercionSpan? Start(
        ActivitySource source,
        RequestContext context,
        OperationType operationType,
        string? operationName,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Variable Coercion");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

        activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operationType]);

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

        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new VariableCoercionSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.VariableValues.Length > 0)
        {
            Activity.MarkAsSuccess();
        }

        enricher.EnrichCoerceVariables(Activity, context);
    }
}
