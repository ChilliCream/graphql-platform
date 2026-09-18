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

        // Cost analysis and other short-circuits can complete the request before the
        // operation is planned and, unlike the compiled plan, the source document is never
        // normalized as a side effect of merely handling the request, so the fallback reads
        // the operation type and name straight from it. The document must already be
        // validated, otherwise a request that never reaches a known operation, such as one
        // that fails document validation, would incorrectly report one.
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
