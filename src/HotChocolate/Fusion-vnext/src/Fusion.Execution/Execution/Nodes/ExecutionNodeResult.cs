using System.Collections.Immutable;
using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed record ExecutionNodeResult(
    int Id,
    Activity? Activity,
    ExecutionStatus Status,
    TimeSpan Duration,
    Exception? Exception,
    ImmutableArray<ExecutionNode> DependentsToExecute,
    ImmutableArray<VariableValues> VariableValueSets);
