using System.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using OpenTelemetry.Trace;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal sealed class ExecuteIntrospectionNodeScope(
    FusionActivityEnricher enricher,
    OperationPlanContext context,
    IntrospectionExecutionNode node,
    Activity activity)
    : NodeScopeBase(enricher, context, activity)
{
    protected override void EnrichActivity()
        => Enricher.EnrichExecuteIntrospectionNode(Context, node, Activity);

    protected override void SetStatus()
    {
        Activity.SetStatus(Status.Ok);
        Activity.SetStatus(ActivityStatusCode.Ok);
    }
}
