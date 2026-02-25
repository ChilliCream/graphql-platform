using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class ExecuteRequestScope(
    FusionActivityEnricher enricher,
    RequestContext context,
    Activity activity)
    : RequestScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichExecuteRequest(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.Result is null or OperationResult { Errors: [_, ..] })
        {
            Activity.SetStatus(Status.Error);
            Activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
