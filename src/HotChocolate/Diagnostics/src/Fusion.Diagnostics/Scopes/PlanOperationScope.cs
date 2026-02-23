using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class PlanOperationScope(
    FusionActivityEnricher enricher,
    RequestContext context,
    Activity activity)
    : RequestScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichPlanOperationScope(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.GetOperationPlan() is not null)
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
