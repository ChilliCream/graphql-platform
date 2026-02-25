using System.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class ExecuteOperationNodeScope(
    FusionActivityEnricher enricher,
    OperationPlanContext context,
    OperationExecutionNode node,
    string schemaName,
    Activity activity)
    : NodeScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichExecuteOperationNode(Context, node, schemaName, Activity);

    protected override void SetStatus()
    {
        Activity.SetStatus(Status.Ok);
        Activity.SetStatus(ActivityStatusCode.Ok);
    }
}
