using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
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
        // An intentional caller cancellation (browser tab closed, connection
        // dropped) is not an error: per the OpenTelemetry semantic conventions
        // the span status is left Unset and no error.type is reported. A
        // server-side execution timeout is not a client cancellation and keeps
        // the regular error behavior below.
        if (ClientCancellation.IsClientCanceled(context))
        {
            // leave the span Unset (neither Error nor Ok).
        }
        else if (context.Result is null
            && context.RequestAborted.IsCancellationRequested
            && context.Features.TryGet<HttpContext>(out var httpContext)
            && !httpContext.RequestAborted.IsCancellationRequested)
        {
            Activity.SetStatus(ActivityStatusCode.Error);
            Activity.SetErrorType(ErrorCodes.Execution.Timeout);
        }
        else if (context.Result is null or OperationResult { Errors: [_, ..] })
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
