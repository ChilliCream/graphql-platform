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

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

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
                Activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operation.Operation]);

                var operationName = operation.Name?.Value;
                if (!string.IsNullOrEmpty(operationName))
                {
                    Activity.SetTag(GraphQL.Operation.Name, operationName);
                }
            }

            Activity.MarkAsSuccess();
        }

        var documentInfo = context.OperationDocumentInfo;

        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            Activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            Activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            Activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.MarkAsError();
        }
        else
        {
            Activity.MarkAsSuccess();
        }

        enricher?.EnrichExecuteRequest(Activity, context);
    }
}
