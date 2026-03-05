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

        return new ParsingSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.TryGetDocument(out _, out _))
        {
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

        enricher.EnrichParseDocument(context, Activity);
    }
}
