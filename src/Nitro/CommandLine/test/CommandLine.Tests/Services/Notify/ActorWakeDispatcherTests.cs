using System.Collections.Concurrent;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ActorWakeDispatcher"/> against a real workspace
/// database, with a scriptable <see cref="FakePingSessionExecutor"/>
/// standing in for the real transports: the claim/no-outstanding-work
/// branch, the no-live-session and mail-already-read fast paths, per-target
/// failure mapping (session gone, no endpoint, unsupported, terminal
/// transport failure), the access-denied offer-and-retain-siblings
/// contract, more than four targets never exceeding four concurrent
/// transports, and losing the batch's own lease renewal mid-dispatch
/// (cancelling in-flight/not-yet-started targets without asserting their
/// outcome) followed by a clean reclaim on the next dispatch.
/// </summary>
public sealed class ActorWakeDispatcherTests : IDisposable
{
    private const string Actor = "codex-worker";
    private const string InstanceId = "host-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly SessionPingGateStore _gates;
    private readonly PingLeaseStore _leases;
    private readonly SessionGateCoordinator _gateCoordinator;
    private readonly FixedInstanceIdProvider _instanceIdProvider = new(InstanceId);
    private readonly FixedGlobalConfigDirectoryProvider _globalConfigDirectoryProvider;

    public ActorWakeDispatcherTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-actor-wake-dispatcher-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            _timeProvider,
            _database,
            _agentRegistry,
            _instanceIdProvider,
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
        _globalConfigDirectoryProvider = new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName);
        _mail = new MailStore(
            _fileSystem, _timeProvider, _database, _agentRegistry, _instanceIdProvider, _globalConfigDirectoryProvider);
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        _gates = new SessionPingGateStore(_fileSystem, _database);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _gateCoordinator = new SessionGateCoordinator(_gates, _leases);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task DispatchAsync_Should_ReturnNull_When_NothingIsOutstanding()
    {
        // arrange: no mail was ever sent with MailWakePolicy.Enqueue, so the
        // actor has no mail_wake_outbox row at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.Null(receipt);
    }

    [Fact]
    public async Task DispatchAsync_Should_CompleteWithNoLiveSessionFailure_When_TheActorHasNoLiveSession()
    {
        // arrange: mail enqueued, but the actor never claimed a live
        // session at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        Assert.Empty(receipt.Targets);

        // the generation settled (nothing durable is left behind): a fresh
        // dispatch with still no live session finds nothing outstanding.
        var again = await dispatcher.DispatchAsync(Actor, cancellationToken);
        Assert.Null(again);
    }

    [Fact]
    public async Task DispatchAsync_Should_SatisfyEveryTarget_When_TheMailWasAlreadyReadBeforeDispatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var message = await SendEnqueuedMailAsync(cancellationToken);
        await _mail.MarkReadAsync([message.Id], Actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert: satisfied, zero, no transport ever attempted, and no
        // second message row was written.
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Satisfied, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(generation, target.Target);
        Assert.Equal(MailWakeTargetStatus.Satisfied, target.Status);
        Assert.Equal("mail-already-read", target.LastError);
        Assert.Empty(executor.Calls);
        var sent = await _mail.QuerySentAsync("pascal", null, cancellationToken);
        Assert.Single(sent);
    }

    [Fact]
    public async Task DispatchAsync_Should_DeliverAndSettleTheBatch_When_TheTransportSucceeds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(generation, target.Target);
        Assert.Equal(MailWakeTargetStatus.Delivered, target.Status);
        Assert.Single(executor.Calls);

        // the batch settled: nothing left outstanding for a fresh dispatch.
        var again = await dispatcher.DispatchAsync(Actor, cancellationToken);
        Assert.Null(again);
    }

    [Fact]
    public async Task DispatchAsync_Should_RecordFailed_When_TheEndpointIsNone()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.None, "", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal("no-endpoint", target.LastError);
        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_RecordFailed_When_TheTransportEndsInAnUnacceptedTerminalFailure()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor { NextReason = PingAttemptReason.Timeout };
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal(PingAttemptReason.Timeout.ToString(), target.LastError);
    }

    [Fact]
    public async Task DispatchAsync_Should_RecordFailed_When_TheFrozenTargetDisappearedBeforeDispatch()
    {
        // arrange: the session materialized into the batch's targets at
        // claim time is gone by the time the target loop re-resolves it
        // (a genuine race in production; forced here through a decorator).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = new ActorWakeDispatcher(
            _batches,
            new AlwaysGoneSessionRegistryDecorator(_sessions),
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal("session-gone", target.LastError);
        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_OfferOnlyTheDeniedTarget_And_LetLiveSiblingsFinish_When_ClaudeAccessIsDenied()
    {
        // arrange: two live sessions, one Claude peer (denied), one Codex
        // thread (delivers normally) - the sibling must still complete.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var claudeGeneration = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken, sessionId: "claude-session");
        var codexGeneration = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken, sessionId: "codex-session");
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        executor.ReasonBySessionId[claudeGeneration.SessionId] = PingAttemptReason.AccessDenied;
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert: pending - one offered (pending, not failed), one
        // delivered; no target actually failed, so the lattice reports
        // pending rather than partial.
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Pending, receipt.Status);
        Assert.Equal(2, receipt.Targets.Count);

        var deniedTarget = Assert.Single(receipt.Targets, t => t.Target == claudeGeneration);
        Assert.Equal(MailWakeTargetStatus.Pending, deniedTarget.Status);
        Assert.Equal("access-denied", deniedTarget.LastError);
        Assert.NotNull(deniedTarget.OfferedGeneration);

        var deliveredTarget = Assert.Single(receipt.Targets, t => t.Target == codexGeneration);
        Assert.Equal(MailWakeTargetStatus.Delivered, deliveredTarget.Status);

        // the denied target's session gate was released without a cooldown:
        // a fresh reservation attempt against it succeeds immediately.
        var retryReservation = await _gateCoordinator.TryReserveAsync(
            claudeGeneration, "retry-attempt", _timeProvider.GetUtcNow(), cancellationToken);
        Assert.NotNull(retryReservation.Reservation);

        // the batch was released with durable offered work, not completed:
        // a fresh dispatch retries it once its rescheduled due_at arrives.
        var immediateRetry = await dispatcher.DispatchAsync(Actor, cancellationToken);
        Assert.Null(immediateRetry);

        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        var rescheduledExecutor = new FakePingSessionExecutor();
        var rescheduledDispatcher = CreateDispatcher(rescheduledExecutor);
        var rescheduled = await rescheduledDispatcher.DispatchAsync(Actor, cancellationToken);
        Assert.NotNull(rescheduled);
    }

    [Fact]
    public async Task DispatchAsync_Should_DispatchEveryTarget_Without_ExceedingFourConcurrentTransports_When_MoreThanFourLiveSessionsExist()
    {
        // arrange: six live sessions for the same actor.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        for (var i = 0; i < 6; i++)
        {
            await SeedLiveSessionAsync(
                AgentSessionEndpointKind.CodexThread, $"thread-{i}", cancellationToken, sessionId: $"session-{i}");
        }

        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor { ConcurrentDelay = TimeSpan.FromMilliseconds(30) };
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(6, receipt.Targets.Count);
        Assert.All(receipt.Targets, t => Assert.Equal(MailWakeTargetStatus.Delivered, t.Status));
        Assert.Equal(6, executor.Calls.Count);
        Assert.InRange(executor.MaxObservedConcurrency, 1, WakeDispatchPolicy.MaxConcurrentTransports);
    }

    [Fact]
    public async Task DispatchAsync_Should_AbandonInFlightTargets_When_TheBatchLeaseRenewalIsLost_And_AllowReclaimOnTheNextDispatch()
    {
        // arrange: a single live session whose transport call hangs until
        // cancelled, standing in for a stalled attempt that never renews in
        // time.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = CreateDispatcher(hangingExecutor);

        // act: start the dispatch, wait until it has actually entered the
        // transport call, then advance the clock well past both the batch's
        // lease duration and its renew interval in one jump, so the
        // renewal loop's very first attempt already finds its own lease
        // expired.
        var dispatchTask = dispatcher.DispatchAsync(Actor, cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchLeaseDuration + TimeSpan.FromSeconds(5));

        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert: never asserted delivered or failed - the target stays
        // pending because this attempt lost its own claim before it could
        // durably record anything.
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // the abandoned batch's row is left exactly as an expired active
        // batch: a fresh dispatch reclaims it (a new batch id) rather than
        // finding it still held.
        var freshExecutor = new FakePingSessionExecutor();
        var freshDispatcher = CreateDispatcher(freshExecutor);
        var reclaimed = await freshDispatcher.DispatchAsync(Actor, cancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(MailWakeTargetStatus.Delivered, reclaimed.Status);
        Assert.Equal(generation, Assert.Single(reclaimed.Targets).Target);
    }

    private ActorWakeDispatcher CreateDispatcher(FakePingSessionExecutor executor)
        => new(
            _batches,
            _sessions,
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<AgentSessionGeneration> SeedLiveSessionAsync(
        string endpointKind,
        string endpointAddr,
        CancellationToken cancellationToken,
        string sessionId = "session-1")
    {
        // A genuinely alive pid/proc_start: dispatch resolves live sessions
        // through FindLiveClaimedByAgentNameAsync, which reaps dead
        // current-instance rows first, so a fake sentinel pid would be
        // reaped out from under the test before it ever reached dispatch.
        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;
        var generation = new AgentSessionGeneration(harness, sessionId, InstanceId, pid, procStart);

        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", endpointKind, endpointAddr,
            envActor: Actor, cancellationToken);

        return generation;
    }

    private async Task<MailMessage> SendEnqueuedMailAsync(CancellationToken cancellationToken)
        => await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "pascal",
                Subject = "status",
                Body = "check",
                To = [Actor],
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);
}

internal sealed record FakePingSessionExecutorCall(string Harness, string SessionId, bool IsClaudePeer);

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

    public PingAttemptReason NextReason { get; set; } = PingAttemptReason.Ok;

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
        => ExecuteAsync(harness, sessionId, attemptId, isClaudePeer: false, cancellationToken);

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string harness,
        string sessionId,
        string actorName,
        int pid,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
        => ExecuteAsync(harness, sessionId, attemptId, isClaudePeer: true, cancellationToken);

    private async Task<PingAttemptOutcome> ExecuteAsync(
        string harness, string sessionId, string attemptId, bool isClaudePeer, CancellationToken cancellationToken)
    {
        Calls.Add(new FakePingSessionExecutorCall(harness, sessionId, isClaudePeer));
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
            Detail: null,
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

/// <summary>
/// Delegates every <see cref="IAgentSessionRegistry"/> member to
/// <paramref name="inner"/> except <see cref="FindByGenerationAsync"/>,
/// which always reports the generation gone - simulating a frozen target
/// that disappeared between batch claim and target dispatch.
/// </summary>
internal sealed class AlwaysGoneSessionRegistryDecorator(IAgentSessionRegistry inner) : IAgentSessionRegistry
{
    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation, string cwd, string workspacePath, string endpointKind,
        string endpointAddr, string? envActor, CancellationToken cancellationToken)
        => inner.StartAsync(generation, cwd, workspacePath, endpointKind, endpointAddr, envActor, cancellationToken);

    public Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation, string actor, bool forceRebind, CancellationToken cancellationToken)
        => inner.ClaimAsync(generation, actor, forceRebind, cancellationToken);

    public Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor, bool forceRebind, CancellationToken cancellationToken)
        => inner.SelfClaimAsync(actor, forceRebind, cancellationToken);

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.EndAsync(generation, cancellationToken);

    public Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
        => Task.FromResult<AgentSessionRecord?>(null);

    public Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.ResetBlockBudgetAsync(generation, cancellationToken);

    public Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.IncrementBlockBudgetAsync(generation, cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken)
        => inner.ReapAsync(cancellationToken);

    public Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken)
        => inner.ListAsync(cancellationToken);

    public Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.TouchAsync(generation, cancellationToken);

    public Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken)
        => inner.RecordHarnessVersionAsync(generation, harnessVersion, cancellationToken);

    public Task<IReadOnlyList<AgentSessionParticipant>> ListParticipantsAsync(CancellationToken cancellationToken)
        => inner.ListParticipantsAsync(cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> FindByProcessAsync(
        string harness, string host, int pid, string procStart, CancellationToken cancellationToken)
        => inner.FindByProcessAsync(harness, host, pid, procStart, cancellationToken);

    public Task<AgentSessionRecord?> FindBySessionIdAsync(
        string harness, string host, string sessionId, CancellationToken cancellationToken)
        => inner.FindBySessionIdAsync(harness, host, sessionId, cancellationToken);

    public Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken)
        => inner.FindLiveClaimedByAgentNameAsync(agentName, cancellationToken);

    public Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session, string attemptId, DateTimeOffset now, TimeSpan cooldown,
        CancellationToken cancellationToken)
        => inner.TryClaimPingCooldownAsync(session, attemptId, now, cooldown, cancellationToken);

    public Task WritePingResultAsync(
        string harness, string sessionId, string attemptId, string result, string? detail,
        CancellationToken cancellationToken)
        => inner.WritePingResultAsync(harness, sessionId, attemptId, result, detail, cancellationToken);

    public Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation, string actor, string role, string client, bool forceRebind,
        CancellationToken cancellationToken)
        => inner.RegisterAsync(generation, actor, role, client, forceRebind, cancellationToken);
}
