using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record OperationPlan
{
    private readonly FrozenDictionary<int, ExecutionNode> _nodes = FrozenDictionary<int, ExecutionNode>.Empty;

    public required Operation Operation { get; init; }

    public required OperationDefinitionNode OperationDefinition { get; init; }

    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions => OperationDefinition.VariableDefinitions;

    public string? OperationName => OperationDefinition.Name?.Value;

    public required ImmutableArray<ExecutionNode> RootNodes { get; init; }

    public required ImmutableArray<ExecutionNode> AllNodes
    {
        get;
        init
        {
            field = value;
            _nodes = value.ToFrozenDictionary(t => t.Id);
        }
    }

    public ExecutionNode GetNodeById(int id)
        => _nodes[id];
}

public sealed class OperationPlanTrace
{
    public string? TraceId { get; init; }

    public string? AppId { get; init; }

    public string? EnvironmentName { get; init; }

    public required TimeSpan Duration { get; init; }

    public ImmutableDictionary<int, ExecutionNodeTrace> Nodes { get; init; } =
        ImmutableDictionary<int, ExecutionNodeTrace>.Empty;
}

public sealed class ExecutionNodeTrace
{
    public required int Id { get; init; }

    public string? SpanId { get; init; }

    public required TimeSpan Duration { get; init; }

    public required ExecutionStatus Status { get; init; }
}
