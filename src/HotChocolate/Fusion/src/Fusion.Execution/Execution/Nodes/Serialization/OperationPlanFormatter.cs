namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public abstract class OperationPlanFormatter
{
    public abstract string Format(OperationPlan plan, OperationPlanTrace? trace = null);
}
