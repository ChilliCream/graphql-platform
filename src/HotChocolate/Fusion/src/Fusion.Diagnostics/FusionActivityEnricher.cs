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
        RequestContext context,
        string operationPlanId,
        Activity activity)
    {
    }

    public virtual void EnrichExecutePlanNode(
        OperationPlanContext context,
        ExecutionNode node,
        string? schemaName,
        Activity activity)
    {
    }

    public virtual void EnrichExecutionNodeError(
        OperationPlanContext context,
        ExecutionNode node,
        Exception exception,
        Activity activity)
    {
    }

    public virtual void EnrichSourceSchemaTransportError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception exception,
        Activity activity)
    {
    }

    public virtual void EnrichSourceSchemaStoreError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Exception exception,
        Activity activity)
    {
    }

    public virtual void EnrichOnSubscriptionEvent(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Activity activity)
    {
    }

    public virtual void EnrichSubscriptionEventError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        ulong subscriptionId,
        Exception exception,
        Activity featureActivity)
    {
    }
}
