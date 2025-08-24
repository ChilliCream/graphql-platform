using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Diagnostics;

public interface IFusionExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents
{
    IDisposable PlanOperation(
        RequestContext context);

    IDisposable ExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node);

    IDisposable ExecuteSubscriptionNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        ulong subscriptionId);

    IDisposable ExecuteIntrospectionNode(
        OperationPlanContext context,
        IntrospectionExecutionNode node);
}
