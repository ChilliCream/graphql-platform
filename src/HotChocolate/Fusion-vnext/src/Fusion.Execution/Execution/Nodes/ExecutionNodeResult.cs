using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record ExecutionNodeResult(
    int Id,
    Activity? Activity,
    ExecutionStatus Status,
    TimeSpan Duration,
    Exception? Exception = null);
