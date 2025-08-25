using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ExecutionNodeTrace
{
    public required int Id { get; init; }

    public string? SpanId { get; init; }

    public required TimeSpan Duration { get; init; }

    public required ExecutionStatus Status { get; init; }

    public required ImmutableArray<VariableValues> VariableSets { get; init; }

    public required string? SchemaName { get; init; }
}
