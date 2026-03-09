using System.Diagnostics;
using HotChocolate.Execution;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class CoerceVariablesScope(
    FusionActivityEnricher enricher,
    RequestContext context,
    Activity activity)
    : RequestScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichCoerceVariables(Context, Activity);

    protected override void SetStatus()
    {
        if (Context.VariableValues.Length > 0)
        {
            Activity.SetStatus(Status.Ok);
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}
