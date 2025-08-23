using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class ExecutionNodeTrace
{
    public required int Id { get; init; }

    public string? SpanId { get; init; }

    public required TimeSpan Duration { get; init; }

    public required ExecutionStatus Status { get; init; }

    public ImmutableArray<VariableValues> VariableSets { get; init; }
}
