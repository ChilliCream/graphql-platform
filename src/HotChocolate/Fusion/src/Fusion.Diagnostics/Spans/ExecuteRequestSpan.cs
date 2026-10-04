using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteRequestSpan(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase enricher,
    bool shouldDisposeActivity)
    : ExecuteRequestSpanBase(activity, context, options, enricher, shouldDisposeActivity)
{
    public static ExecuteRequestSpan? Start(
        ActivitySource source,
        RequestContext context,
        InstrumentationOptionsBase options,
        ActivityEnricherBase enricher)
    {
        var activity = StartActivity(source);

        if (activity is null)
        {
            return null;
        }

        return new ExecuteRequestSpan(
            activity,
            context,
            options,
            enricher,
            true);
    }

    protected override bool TryGetOperationInfo(
        out OperationType operationType,
        out string? operationName)
    {
        if (Context.GetOperationPlan() is { Operation: var operation })
        {
            operationType = operation.Definition.Operation;
            operationName = operation.Name;
            return true;
        }

        // Cost rejection can finish a request before planning.
        // Use only validated documents when reporting operation details.
        if (Context.OperationDocumentInfo is { IsValidated: true, Document: { } document }
            && document.TryGetOperationDefinition(Context.Request.OperationName, out var operationDefinition))
        {
            operationType = operationDefinition.Operation;
            operationName = operationDefinition.Name?.Value;
            return true;
        }

        operationType = default;
        operationName = null;
        return false;
    }
}
