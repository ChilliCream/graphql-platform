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
    ActivityEnricherBase? enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public RequestContext Context { get; } = context;

    protected static Activity? StartActivity(ActivitySource source)
    {
        return source.StartActivity("GraphQL Operation", ActivityKind.Server);
    }

    protected abstract bool TryGetOperationInfo(
        out OperationType operationType,
        out string? operationName);

    protected override void OnComplete()
    {
        if (TryGetOperationInfo(out var operationType, out var operationName))
        {
            var operationTypeValue = GraphQL.Operation.TypeValues[operationType];
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
            Activity.SetTag(GraphQL.Errors.Count, result.Errors.Count);
        }

        if (Context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);
        }
        else if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher?.EnrichExecuteRequest(Context, Activity);
    }
}
