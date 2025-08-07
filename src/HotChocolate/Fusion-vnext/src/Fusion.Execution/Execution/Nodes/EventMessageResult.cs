using System.Diagnostics;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed record EventMessageResult(
    int Id,
    Activity? Activity,
    ExecutionStatus Status,
    IDisposable Scope,
    long StartTimestamp,
    long EndTimestamp,
    Exception? Exception = null)
    : IDisposable
{
    public TimeSpan Duration => Stopwatch.GetElapsedTime(StartTimestamp, EndTimestamp);

    public void Dispose() => Scope.Dispose();
}
