namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public abstract class OperationPlanParser
{
    public abstract OperationPlan Parse(ReadOnlyMemory<byte> planSourceText);
}
