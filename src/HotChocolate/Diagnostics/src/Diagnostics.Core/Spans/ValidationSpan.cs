using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ValidationSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ValidationSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity( "GraphQL Document Validation");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        if (context.TryGetDocument(out var document, out _)
            && document.GetOperation(context.Request.OperationName) is { } operation)
        {
            activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operation.Operation]);

            var operationName = operation.Name?.Value;
            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        var hash = documentInfo.Hash;

        if (!hash.IsEmpty)
        {
            activity.SetTag(GraphQL.Document.Hash, $"{hash.AlgorithmName}:{hash.Value}");
        }

        if (documentInfo.IsPersisted && documentInfo.Id.HasValue)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        return new ValidationSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.IsOperationDocumentValid())
        {
            Activity.MarkAsSuccess();
        }

        enricher.EnrichValidateDocument(Activity, context);
    }
}
