using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class ParseDocumentScope : RequestScopeBase
{
    public ParseDocumentScope(
        FusionActivityEnricher enricher,
        RequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichParseDocument(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.TryGetOperationDocument(out _, out _))
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
