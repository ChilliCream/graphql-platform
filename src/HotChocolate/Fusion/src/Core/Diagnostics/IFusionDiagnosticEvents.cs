using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Diagnostics;

internal interface IFusionDiagnosticEvents
{
    IDisposable BeginExecutePlan(FusionExecutionContext context, QueryPlan node);
    
    IDisposable BeginExecuteNode(FusionExecutionContext context, QueryPlanNode node);
    
    void NodeExecutionError(FusionExecutionContext context, QueryPlanNode node, Exception exception);
}

internal sealed class NoOpFusionDiagnosticEventListener : FusionDiagnosticEventListener;

internal abstract class FusionDiagnosticEventListener : IFusionDiagnosticEvents
{
    public IDisposable BeginExecutePlan(FusionExecutionContext context, QueryPlan node)
    {
        throw new NotImplementedException();
    }

    public IDisposable BeginExecuteNode(FusionExecutionContext context, QueryPlanNode node)
    {
        throw new NotImplementedException();
    }

    public void NodeExecutionError(FusionExecutionContext context, QueryPlanNode node, Exception exception)
    {
        throw new NotImplementedException();
    }
   
   
    protected static IDisposable NoSession { get; } = new NoOpSession();
    
    private sealed class NoOpSession : IDisposable
    {
        public void Dispose()
        {
            // we do nothing as this is just a stub
        }
    }

    
} 