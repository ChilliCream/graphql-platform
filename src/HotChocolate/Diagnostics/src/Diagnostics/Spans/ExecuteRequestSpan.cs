using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteRequestSpan : ExecuteRequestSpanBase
{
    public ExecuteRequestSpan(
        Activity activity,
        RequestContext context,
        InstrumentationOptionsBase options,
        ActivityEnricherBase enricher,
        bool shouldDisposeActivity)
        : base(activity, context, options, enricher, shouldDisposeActivity)
    {
    }

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
        if (Context.TryGetOperation(out var operation))
        {
            operationType = operation.Kind;
            operationName = operation.Name;
            return true;
        }
        else if (Context.OperationDocumentInfo.NormalizedDocument
            is { Definitions: [OperationDefinitionNode normalizedOperation] })
        {
            operationType = normalizedOperation.Operation;
            operationName = normalizedOperation.Name?.Value;
            return true;
        }
        else if (Context.OperationDocumentInfo is { IsValidated: true, Document: { } document }
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
