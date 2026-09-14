using System.Collections.Concurrent;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// A scriptable <see cref="IPingSessionExecutor"/>: returns
/// <see cref="PingAttemptReason.Ok"/> by default, a fixed
/// <see cref="NextReason"/> or a per-session override from
/// <see cref="ReasonBySessionId"/> otherwise, optionally hangs until its own
/// cancellation token fires (<see cref="HangUntilCancelled"/>, signalling
/// <see cref="Entered"/> once invoked), and tracks the highest number of
/// concurrently in-flight calls it observed.
/// </summary>
internal sealed class FakePingSessionExecutor : IPingSessionExecutor
{
    private int _concurrent;

    public ConcurrentBag<FakePingSessionExecutorCall> Calls { get; } = [];

    public ConcurrentBag<DateTimeOffset> RecordedDeadlines { get; } = [];

    public PingAttemptReason NextReason { get; set; } = PingAttemptReason.Ok;

    public string? NextDetail { get; set; }

    public ConcurrentDictionary<string, PingAttemptReason> ReasonBySessionId { get; } = new();

    public TimeSpan ConcurrentDelay { get; set; } = TimeSpan.Zero;

    public bool HangUntilCancelled { get; set; }

    private int _maxObservedConcurrency;

    public int MaxObservedConcurrency => _maxObservedConcurrency;

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<PingAttemptOutcome> ExecuteCodexThreadAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(
            harness, sessionId, attemptId, isClaudePeer: false, isOpencodeServer: false, cancellationToken);
    }

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string harness,
        string sessionId,
        string actorName,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(
            harness, sessionId, attemptId, isClaudePeer: true, isOpencodeServer: false, cancellationToken);
    }

    public Task<PingAttemptOutcome> ExecuteOpencodeServerAsync(
        string harness,
        string sessionId,
        string actorName,
        string endpointAddr,
        string? endpointSecret,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(
            harness, sessionId, attemptId, isClaudePeer: false, isOpencodeServer: true, cancellationToken);
    }

    private async Task<PingAttemptOutcome> ExecuteAsync(
        string harness,
        string sessionId,
        string attemptId,
        bool isClaudePeer,
        bool isOpencodeServer,
        CancellationToken cancellationToken)
    {
        Calls.Add(new FakePingSessionExecutorCall(harness, sessionId, isClaudePeer, isOpencodeServer));
        Entered.TrySetResult();

        var observed = Interlocked.Increment(ref _concurrent);
        InterlockedMax(ref _maxObservedConcurrency, observed);

        try
        {
            if (HangUntilCancelled)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            else if (ConcurrentDelay > TimeSpan.Zero)
            {
                await Task.Delay(ConcurrentDelay, cancellationToken);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _concurrent);
        }

        var reason = ReasonBySessionId.GetValueOrDefault(sessionId, NextReason);

        return new PingAttemptOutcome(
            reason == PingAttemptReason.Ok ? AgentPingResult.Ok : AgentPingResult.Error,
            reason,
            Retryable: reason is PingAttemptReason.Timeout or PingAttemptReason.TransportError,
            Detail: NextDetail,
            harness,
            sessionId,
            attemptId,
            DateTimeOffset.UtcNow);
    }

    private static void InterlockedMax(ref int target, int observed)
    {
        int current;

        do
        {
            current = target;

            if (observed <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, observed, current) != current);
    }
}
