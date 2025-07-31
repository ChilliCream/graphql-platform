using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Diagnostics;

public interface IFusionExecutionDiagnosticEvents : ICoreExecutionDiagnosticEvents
{
    IDisposable ExecuteOperation(
        OperationPlanContext context,
        OperationExecutionNode node);

    IDisposable ExecuteSubscriptionEvent(
        OperationPlanContext context,
        OperationExecutionNode node);

    IDisposable ExecuteIntrospection(
        OperationPlanContext context,
        IntrospectionExecutionNode node);
}
