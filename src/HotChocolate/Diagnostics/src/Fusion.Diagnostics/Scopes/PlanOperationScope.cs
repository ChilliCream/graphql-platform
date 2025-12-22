using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class PlanOperationScope : RequestScopeBase
{
    public PlanOperationScope(
        FusionActivityEnricher enricher,
        RequestContext context,
        Activity activity)
        : base(enricher, context, activity)
    {
    }

    protected override void EnrichActivity()
        => Enricher.EnrichCompileOperation(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.GetOperationPlan() is not null)
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
