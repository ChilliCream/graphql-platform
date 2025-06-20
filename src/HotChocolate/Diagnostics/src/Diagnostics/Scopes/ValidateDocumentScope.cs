using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class ValidateDocumentScope : RequestScopeBase
{
    public ValidateDocumentScope(
        ActivityEnricher enricher,
        RequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichValidateDocument(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.IsOperationDocumentValid())
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
