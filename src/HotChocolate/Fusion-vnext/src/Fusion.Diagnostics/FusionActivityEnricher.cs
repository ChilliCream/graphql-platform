using System.Diagnostics;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// The activity enricher allows adding additional information to the activity spans
/// created by the Fusion diagnostics system.
/// You can inherit from this class and override the enricher methods to add
/// additional information to the spans.
/// </summary>
public class FusionActivityEnricher(InstrumentationOptions options) : ActivityEnricherBase
{
    protected InstrumentationOptions Options { get; } = options;

    public virtual void EnrichPlanOperation(
        Activity activity,
        RequestContext context,
        string operationPlanId) { }

    public virtual void EnrichExecutePlanNode(
        Activity activity,
        OperationPlanContext context,
        ExecutionNode node,
        string? schemaName) { }

    public virtual void EnrichExecutionNodeError(
        Activity activity,
        OperationPlanContext context,
        ExecutionNode node,
        Exception exception) { }

    public virtual void EnrichSourceSchemaTransportError(
        Activity activity,
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception exception) { }

    public virtual void EnrichSourceSchemaStoreError(
        Activity activity,
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception exception) { }

    public virtual void EnrichOnSubscriptionEvent(
        Activity activity,
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId) { }
}
