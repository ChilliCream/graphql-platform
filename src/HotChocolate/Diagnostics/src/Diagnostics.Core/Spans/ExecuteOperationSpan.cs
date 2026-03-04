using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteOperationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ExecuteOperationSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Operation Execution");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        // TODO: This should get it from the operation
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

        return new ExecuteOperationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.MarkAsError();
        }
        else
        {
            Activity.MarkAsSuccess();
        }

        enricher.EnrichExecuteOperation(Activity, context);
    }
}
