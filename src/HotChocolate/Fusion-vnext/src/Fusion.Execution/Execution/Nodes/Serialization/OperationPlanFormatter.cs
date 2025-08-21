namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public abstract class OperationPlanFormatter
{
    public abstract string Format(OperationPlan plan, OperationPlanTrace? trace = null);
}

public abstract class OperationPlanParser
{
    public abstract OperationPlan Parse(ReadOnlyMemory<byte> planSourceText);
}
