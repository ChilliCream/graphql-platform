using System.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class ExecuteNodeFieldNodeScope(
    FusionActivityEnricher enricher,
    OperationPlanContext context,
    NodeFieldExecutionNode node,
    Activity activity)
    : NodeScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichExecuteNodeFieldNode(Context, node, Activity);

    protected override void SetStatus()
    {
        Activity.SetStatus(Status.Ok);
        Activity.SetStatus(ActivityStatusCode.Ok);
    }
}
