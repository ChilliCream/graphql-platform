namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ExecutionNodeCondition
{
    public required string VariableName { get; init; }

    public required bool PassingValue { get; init; }
}
