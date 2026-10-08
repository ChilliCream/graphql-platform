using System.Collections.Concurrent;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// A scriptable <see cref="IPingSessionExecutor"/> whose per-actor
/// <see cref="ReasonByActor"/> entries override <see cref="NextReason"/>, which defaults to
/// <see cref="PingAttemptReason.Ok"/>. Signals <see cref="Entered"/> on entry and waits
/// for cancellation when <see cref="HangUntilCancelled"/> is set.
/// </summary>
internal sealed class FakePingSessionExecutor : IPingSessionExecutor
{
    private int _concurrent;

    public ConcurrentBag<FakePingSessionExecutorCall> Calls { get; } = [];

    public ConcurrentBag<DateTimeOffset> RecordedDeadlines { get; } = [];

    public PingAttemptReason NextReason { get; set; } = PingAttemptReason.Ok;

    public string? NextDetail { get; set; }

    public ConcurrentDictionary<string, PingAttemptReason> ReasonByActor { get; } = new();

    public TimeSpan ConcurrentDelay { get; set; } = TimeSpan.Zero;

    public bool HangUntilCancelled { get; set; }

    private int _maxObservedConcurrency;

    public int MaxObservedConcurrency => _maxObservedConcurrency;

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<PingAttemptOutcome> ExecuteCodexThreadAsync(
        string actorName,
        string endpointAddr,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(actorName, attemptId, isClaudePeer: false, isOpencodeServer: false, cancellationToken);
    }

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string actorName,
        string sessionId,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(actorName, attemptId, isClaudePeer: true, isOpencodeServer: false, cancellationToken);
    }

    public Task<PingAttemptOutcome> ExecuteOpencodeServerAsync(
        string actorName,
        string sessionId,
        string endpointAddr,
        string? endpointSecret,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(actorName, attemptId, isClaudePeer: false, isOpencodeServer: true, cancellationToken);
    }

    private async Task<PingAttemptOutcome> ExecuteAsync(
        string actorName,
        string attemptId,
        bool isClaudePeer,
        bool isOpencodeServer,
        CancellationToken cancellationToken)
    {
        Calls.Add(new FakePingSessionExecutorCall(actorName, isClaudePeer, isOpencodeServer));
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

        var reason = ReasonByActor.GetValueOrDefault(actorName, NextReason);

        return new PingAttemptOutcome(
            reason == PingAttemptReason.Ok ? AgentPingResult.Ok : AgentPingResult.Error,
            reason,
            Retryable: reason is PingAttemptReason.Timeout or PingAttemptReason.TransportError,
            Detail: NextDetail,
            actorName,
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
