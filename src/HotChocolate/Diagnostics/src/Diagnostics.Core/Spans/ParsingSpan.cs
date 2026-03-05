using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ParsingSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static ParsingSpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Document Parsing");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        return new ParsingSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
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

        enricher.EnrichParseDocument(Activity, context);
    }
}
