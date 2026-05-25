using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
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

        activity.EnrichOperation(operationType, operationName);
        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new ExecuteOperationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);

            if (context.Result is OperationResult { Errors: [var firstError, ..] })
            {
                Activity.SetErrorType(firstError, ActivityExtensions.ExecutionErrorType);
            }
            else
            {
                Activity.SetErrorType(ActivityExtensions.ExecutionErrorType);
            }
        }
        else
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichExecuteOperation(context, Activity);
    }
}
