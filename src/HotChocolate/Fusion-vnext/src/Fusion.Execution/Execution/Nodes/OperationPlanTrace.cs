using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationPlanTrace
{
    public string? TraceId { get; init; }

    public string? AppId { get; init; }

    public string? EnvironmentName { get; init; }

    public required TimeSpan Duration { get; init; }

    public ImmutableDictionary<int, ExecutionNodeTrace> Nodes { get; init; } =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<int, ExecutionNodeTrace>.Empty;
#endif
}
