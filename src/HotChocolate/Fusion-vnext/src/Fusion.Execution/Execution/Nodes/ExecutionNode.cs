namespace HotChocolate.Fusion.Execution.Nodes;

public abstract class ExecutionNode : IEquatable<ExecutionNode>
{
    public abstract int Id { get; }

    public abstract ReadOnlySpan<ExecutionNode> Dependencies { get; }

    public abstract Task<ExecutionStatus> ExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default);

    public bool Equals(ExecutionNode? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? obj)
        => Equals(obj as ExecutionNode);

    public override int GetHashCode()
        => Id;

    protected internal abstract void Seal();
}
