using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteOperationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity, shouldDisposeActivity: false)
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
        enricher.EnrichExecuteOperation(context, Activity);

        // When the operation is torn down by an in-flight cancellation the result
        // is not yet known here: a client abort and a server-side execution timeout
        // both unwind as a cancellation with no result set. The two cases require
        // opposite span statuses (Unset vs. Error), so defer the decision to the
        // root request span, which observes the final result and finalizes this
        // span once the outcome is known.
        if (context.Result is null && context.RequestAborted.IsCancellationRequested)
        {
            context.Features.GetOrSet<DeferredOperationSpans>().Add(this);
            return;
        }

        Classify();
        Activity.Dispose();
    }

    /// <summary>
    /// Applies the terminal span status once the request outcome is known.
    /// </summary>
    private void Classify()
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
    }

    /// <summary>
    /// Holds operation spans whose terminal status cannot be decided when they
    /// complete because the request outcome (client cancellation vs. execution
    /// timeout) is not yet known.
    /// </summary>
    internal sealed class DeferredOperationSpans
    {
        private readonly List<ExecuteOperationSpan> _spans = [];

        public void Add(ExecuteOperationSpan span) => _spans.Add(span);

        public void Complete()
        {
            foreach (var span in _spans)
            {
                span.Classify();
                span.Activity.Dispose();
            }

            _spans.Clear();
        }
    }
}
