using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal abstract class ExecuteRequestSpanBase(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public RequestContext Context { get; } = context;

    protected static Activity? StartActivity(ActivitySource source)
    {
        var activity = source.StartActivity("GraphQL Operation");

        activity?.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Request);

        return activity;
    }

    protected abstract bool TryGetOperationInfo(
        out OperationType operationType,
        out string? operationName);

    protected override void OnComplete()
    {
        // Ensures the root request span carries `graphql.processing.type=request`
        // even when an existing HTTP transport activity was reused as the root.
        if (Activity.GetTagItem(GraphQL.Processing.Type) is null)
        {
            Activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Request);
        }

        string? operationTypeValue = null;
        string? operationName = null;
        if (TryGetOperationInfo(out var operationType, out operationName))
        {
            operationTypeValue = GraphQL.Operation.TypeValues[operationType];
            Activity.DisplayName = operationTypeValue;
            Activity.EnrichOperation(operationType, operationName);
        }

        var documentInfo = Context.OperationDocumentInfo;

        Activity.EnrichDocumentInfo(documentInfo);

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            Activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (Context.Result is OperationResult result)
        {
            // This was previously also always set to 0, so I just kept that behavior.
            Activity.SetTag(GraphQL.Error.Count, result.Errors.Count);

            EmitErrorEvents(result.Errors, operationTypeValue, operationName);
        }

        if (Context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);

            if (Activity.GetTagItem(SemanticConventions.ErrorType) is null)
            {
                if (Context.Result is OperationResult { Errors: [var firstError, ..] })
                {
                    Activity.SetGraphQLErrorType(firstError, ActivityExtensions.ExecutionErrorType);
                }
                else
                {
                    Activity.SetTag(SemanticConventions.ErrorType, ActivityExtensions.ExecutionErrorType);
                }
            }
        }
        else if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichExecuteRequest(Context, Activity);
    }

    private void EmitErrorEvents(
        IReadOnlyList<IError> errors,
        string? operationType,
        string? operationName)
    {
        var maxEvents = options.MaxErrorEvents;
        if (maxEvents <= 0 || errors.Count == 0)
        {
            return;
        }

        var limit = errors.Count < maxEvents ? errors.Count : maxEvents;
        for (var i = 0; i < limit; i++)
        {
            Activity.AddGraphQLErrorEvent(errors[i], operationType, operationName);
        }
    }
}
