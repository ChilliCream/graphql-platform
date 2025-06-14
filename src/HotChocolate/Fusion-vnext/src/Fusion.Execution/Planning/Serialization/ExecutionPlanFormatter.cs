using System.Text;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Planning;

public abstract class ExecutionPlanFormatter
{
    public abstract string Format(OperationExecutionPlan plan);
}
