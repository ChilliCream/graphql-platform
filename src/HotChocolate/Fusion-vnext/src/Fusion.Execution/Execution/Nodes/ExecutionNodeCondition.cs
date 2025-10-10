using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ExecutionNodeCondition
{
    public required string VariableName { get; init; }

    public required bool PassingValue { get; init; }

    public DirectiveNode? Directive { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is not ExecutionNodeCondition other)
        {
            return false;
        }

        return other.VariableName == VariableName
            && other.PassingValue == PassingValue;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VariableName, PassingValue);
    }
}
