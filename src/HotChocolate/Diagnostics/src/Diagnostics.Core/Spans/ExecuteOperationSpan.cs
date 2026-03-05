using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
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
        OperationType operationType,
        string? operationName,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Operation Execution");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

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

        if (documentInfo is { IsPersisted: true, Id.HasValue: true })
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
