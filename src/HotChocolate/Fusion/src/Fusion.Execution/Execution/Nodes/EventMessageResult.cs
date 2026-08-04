using System.Collections.Immutable;
using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

internal sealed record EventMessageResult(
    int Id,
    Activity? Activity,
    ExecutionStatus Status,
    IDisposable Scope,
    long StartTimestamp,
    long EndTimestamp,
    Exception? Exception,
    ImmutableArray<VariableValues> VariableValueSets,
    ImmutableArray<ExecutionNode> DependentsToExecute = default)
    : IDisposable
{
    public TimeSpan Duration => Stopwatch.GetElapsedTime(StartTimestamp, EndTimestamp);

    public void Dispose() => Scope.Dispose();
}
