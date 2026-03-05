using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteRequestSpan(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase? enricher,
    bool shouldDisposeActivity) : SpanBase(activity, shouldDisposeActivity)
{
    public static ExecuteRequestSpan? Start(
        ActivitySource source,
        RequestContext context,
        InstrumentationOptionsBase options,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Operation", ActivityKind.Server);

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

    protected override void OnComplete()
    {
        // TODO: This could be faster through the operation
        if (context.TryGetDocument(out var document, out _))
        {
            if (document.GetOperation(context.Request.OperationName) is { } operation)
            {
                var operationType = GraphQL.Operation.TypeValues[operation.Operation];

                Activity.DisplayName = operationType;
                Activity.SetTag(GraphQL.Operation.Type, operationType);

                var operationName = operation.Name?.Value;
                if (!string.IsNullOrEmpty(operationName))
                {
                    Activity.SetTag(GraphQL.Operation.Name, operationName);
                }
            }

            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        var documentInfo = context.OperationDocumentInfo;

        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            Activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo is { IsPersisted: true, Id.HasValue: true })
        {
            Activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            Activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(ActivityStatusCode.Error);
        }
        else
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher?.EnrichExecuteRequest(Activity, context);
    }
}
