namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Evaluates authorization policies for one or more result positions.
/// </summary>
public sealed class PolicyExecutionNode : ExecutionNode
{
    private readonly PolicyExecutionTarget[] _targets;
    private readonly ExecutionNodeCondition[] _conditions;

    internal PolicyExecutionNode(
        int id,
        PolicyExecutionTarget[] targets,
        ExecutionNodeCondition[] conditions)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(conditions);

        Id = id;
        _targets = targets;
        _conditions = conditions;
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.Policy;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    public override string? SchemaName => null;

    /// <summary>
    /// Gets the policy targets evaluated by this node.
    /// </summary>
    public ReadOnlySpan<PolicyExecutionTarget> Targets => _targets;

    protected override ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ExecutionStatus.Success);
}
