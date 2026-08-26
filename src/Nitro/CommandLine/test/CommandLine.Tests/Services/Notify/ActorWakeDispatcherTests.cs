using System.Collections.Concurrent;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        Assert.Empty(receipt.Targets);

        // the generation settled (nothing durable is left behind): a fresh
        // dispatch with still no live session finds nothing outstanding.
        var again = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(generation, target.Target);
        Assert.Equal(MailWakeTargetStatus.Delivered, target.Status);
        Assert.Single(executor.Calls);

        // the batch settled: nothing left outstanding for a fresh dispatch.
        var again = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal("session-gone", target.LastError);
        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_RecordFailed_And_LeaveTheReboundGenerationUntouched_When_TheFrozenTargetFullyRebounds()
    {
        // arrange: a genuine full-session rebound - the frozen target's
        // session actually ends and a new process claims the same
        // (harness, session_id) under a new pid/proc_start - races the
        // target loop's own re-resolution of the frozen generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var frozen = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var rebound = frozen with { Pid = 999_999, ProcStart = "999999" };
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = new ActorWakeDispatcher(
            _batches,
            new ReboundOnFindSessionRegistryDecorator(_sessions, frozen, rebound, Actor),
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert: the frozen generation fails as session-gone, no transport
        // was ever attempted against it, and the row the rebound generation
        // now owns is completely untouched by this batch.
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(frozen, target.Target);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal("session-gone", target.LastError);
        Assert.Empty(executor.Calls);

        var reboundSession = await _sessions.FindByGenerationAsync(rebound, cancellationToken);
        Assert.NotNull(reboundSession);
        Assert.Equal(Actor, reboundSession.AgentName);
        Assert.Equal(0, reboundSession.BlockBudgetUsed);
    }

    [Fact]
    public async Task DispatchAsync_Should_ClampTheAttemptDeadline_When_TheSharedDeadlineIsAlreadyWithinTheHandoffReserve()
    {
        // arrange: a caller-supplied deadline only 200ms out, well inside
        // WakeDispatchPolicy.HandoffObservationReserve (500ms), so the
        // reserve alone pushes the clamped attempt deadline before now.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);
        var tightDeadline = _timeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(200);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, tightDeadline, cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var recordedDeadline = Assert.Single(executor.RecordedDeadlines);
        Assert.True(
            recordedDeadline <= tightDeadline - WakeDispatchPolicy.HandoffObservationReserve,
            $"Expected {recordedDeadline} to be no later than "
            + $"{tightDeadline - WakeDispatchPolicy.HandoffObservationReserve}.");
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
        var dispatchTask = dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
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
        var reclaimed = await freshDispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(MailWakeTargetStatus.Delivered, reclaimed.Status);
        Assert.Equal(generation, Assert.Single(reclaimed.Targets).Target);
    }

    [Fact]
    public async Task DispatchAsync_Should_AbandonInFlightTargets_When_TheBatchRenewalThrows_And_AllowReclaimOnceTheLeaseExpires()
    {
        // arrange: TryRenewAsync itself throws (the store call failed
        // outright, not merely reported false) - a renewal whose result is
        // unknown must be treated exactly like a lost renewal, never let a
        // non-OperationCanceledException escape the renewal loop.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = new ActorWakeDispatcher(
            new ThrowingRenewMailWakeBatchStore(_batches),
            _sessions,
            _gateCoordinator,
            hangingExecutor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act: start the dispatch, wait until it has entered the transport
        // call, then advance the clock just past the renew interval - well
        // short of the batch's own 30s lease - so it is the renewal
        // attempt's own thrown exception that ends dispatch, not a natural
        // lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchRenewInterval + TimeSpan.FromSeconds(1));

        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert: never asserted delivered or failed - the target stays
        // pending because this attempt never durably recorded anything
        // before its renewal failed.
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // the abandoned batch's row was never renewed, so once its original
        // lease fully expires a fresh dispatch (through the real store)
        // reclaims it.
        _timeProvider.Advance(WakeDispatchPolicy.BatchLeaseDuration);
        var freshExecutor = new FakePingSessionExecutor();
        var freshDispatcher = CreateDispatcher(freshExecutor);
        var reclaimed = await freshDispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(MailWakeTargetStatus.Delivered, reclaimed.Status);
        Assert.Equal(generation, Assert.Single(reclaimed.Targets).Target);
    }

    [Fact]
    public async Task DispatchAsync_Should_ReturnNull_And_LeaveTheOtherInstancesWorkUntouched_When_TheNitroInstanceIdDiffers()
    {
        // arrange: outstanding wake work claimed under this test class's own
        // instance id ("host-1"); a dispatcher for a different Nitro
        // instance id must never see or touch it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var otherInstanceDispatcher = new ActorWakeDispatcher(
            _batches,
            _sessions,
            _gateCoordinator,
            new FakePingSessionExecutor(),
            _mail,
            new FixedInstanceIdProvider("host-2"),
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act
        var receipt = await otherInstanceDispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert: nothing claimed for host-2, and host-1's own outstanding
        // work is completely unaffected - a same-instance dispatcher still
        // claims it normally afterward.
        Assert.Null(receipt);

        var hostOneDispatcher = CreateDispatcher(new FakePingSessionExecutor());
        var hostOneReceipt = await hostOneDispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        Assert.NotNull(hostOneReceipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, hostOneReceipt.Status);
    }

    [Fact]
    public async Task DispatchAsync_Should_ReportFailed_When_ATerminalFailureRacesAConcurrentlyRecordedAcceptance()
    {
        // arrange: the transport ends in an unaccepted terminal failure, but
        // a decorator races a synthetic Delivered acceptance for the same
        // target through this batch's own owner/attempt fence just before
        // the dispatcher's own terminal write commits - the terminal outcome
        // must still be what the receipt (and the durable row) report.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor { NextReason = PingAttemptReason.Timeout };
        var racingBatches = new RacingAcceptanceMailWakeBatchStoreDecorator(_batches, generation, acceptedGeneration: 1);
        var dispatcher = new ActorWakeDispatcher(
            racingBatches,
            _sessions,
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal(PingAttemptReason.Timeout.ToString(), target.LastError);

        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM mail_wake_targets WHERE session_id = @sessionId";
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);
        var status = (string)(await command.ExecuteScalarAsync(cancellationToken))!;
        Assert.Equal(MailWakeTargetStatus.Failed, status);
    }

    /// <summary>
    /// A board process may run the fallback dispatcher, but is not itself a
    /// recipient target. The one coding session still receives the wake.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_Should_PingOnlyTheCodexTarget_When_TheActorAlsoHasABoardSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var boardGeneration = new AgentSessionGeneration(
            AgentSessionHarness.NitroBoard, "board-1", InstanceId, pid, procStart);
        await _sessions.StartAsync(
            boardGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.DbWatch, "local",
            envActor: "pascal", cancellationToken);

        var codexGeneration = new AgentSessionGeneration(
            AgentSessionHarness.Codex, "codex-session", InstanceId, pid, procStart);
        await _sessions.StartAsync(
            codexGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-1",
            envActor: "pascal", cancellationToken);

        var message = await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "codex-worker",
                Subject = "status",
                Body = "check",
                To = ["pascal"],
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);

        var queueClient = new FakeCodexQueueClient();
        var executor = new PingSessionExecutor(
            _mail, queueClient, new NoopClaudePeerClient(), _sessions, _leases, _timeProvider);
        var dispatcher = new ActorWakeDispatcher(
            _batches,
            _sessions,
            _gateCoordinator,
            executor,
            _mail,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            _timeProvider);

        // act
        var dispatchReceipt = await dispatcher.DispatchAsync("pascal", Deadline(), cancellationToken);

        // assert
        Assert.NotNull(dispatchReceipt);
        var codexTarget = Assert.Single(dispatchReceipt.Targets);
        Assert.Equal(codexGeneration, codexTarget.Target);
        Assert.Equal(MailWakeTargetStatus.Delivered, codexTarget.Status);

        var call = Assert.Single(queueClient.Calls);
        Assert.Equal("thread-1", call.ThreadId);
        Assert.Contains(message.Id, call.Message);
    }

    private static async Task<long> ScalarCountAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        string sql,
        string? sessionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (sessionId is not null)
        {
            command.Parameters.AddWithValue("@sessionId", sessionId);
        }

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private DateTimeOffset Deadline() => _timeProvider.GetUtcNow() + WakeDispatchPolicy.BatchDeadline;

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

    public ConcurrentBag<DateTimeOffset> RecordedDeadlines { get; } = [];

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
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(harness, sessionId, attemptId, isClaudePeer: false, cancellationToken);
    }

    public Task<PingAttemptOutcome> ExecuteClaudePeerAsync(
        string harness,
        string sessionId,
        string actorName,
        int pid,
        string attemptId,
        int slot,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        RecordedDeadlines.Add(deadline);
        return ExecuteAsync(harness, sessionId, attemptId, isClaudePeer: true, cancellationToken);
    }

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

    public Task<bool> SetRoleAsync(AgentSessionGeneration generation, string role, CancellationToken cancellationToken)
        => inner.SetRoleAsync(generation, role, cancellationToken);

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

/// <summary>
/// Delegates every <see cref="IAgentSessionRegistry"/> member to
/// <paramref name="inner"/> except <see cref="FindByGenerationAsync"/>: its
/// first call for <paramref name="frozen"/> first performs a genuine
/// end-then-start-then-claim rebind through <paramref name="inner"/> onto
/// <paramref name="rebound"/> (the same harness/session id, a new
/// pid/proc_start) before delegating, simulating a full session rebound
/// racing the target loop's own re-resolution of its frozen generation.
/// </summary>
internal sealed class ReboundOnFindSessionRegistryDecorator(
    IAgentSessionRegistry inner, AgentSessionGeneration frozen, AgentSessionGeneration rebound, string actor)
    : IAgentSessionRegistry
{
    private bool _rebound;

    public Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation, string cwd, string workspacePath, string endpointKind,
        string endpointAddr, string? envActor, CancellationToken cancellationToken)
        => inner.StartAsync(generation, cwd, workspacePath, endpointKind, endpointAddr, envActor, cancellationToken);

    public Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation, string actor2, bool forceRebind, CancellationToken cancellationToken)
        => inner.ClaimAsync(generation, actor2, forceRebind, cancellationToken);

    public Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor2, bool forceRebind, CancellationToken cancellationToken)
        => inner.SelfClaimAsync(actor2, forceRebind, cancellationToken);

    public Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken)
        => inner.EndAsync(generation, cancellationToken);

    public async Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken)
    {
        if (!_rebound && generation == frozen)
        {
            _rebound = true;
            await inner.EndAsync(frozen, cancellationToken);
            await inner.StartAsync(
                rebound, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.CodexThread, "thread-rebound",
                envActor: actor, cancellationToken);
            await inner.ClaimAsync(rebound, actor, forceRebind: false, cancellationToken);
        }

        return await inner.FindByGenerationAsync(generation, cancellationToken);
    }

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

    public Task<bool> SetRoleAsync(AgentSessionGeneration generation, string role, CancellationToken cancellationToken)
        => inner.SetRoleAsync(generation, role, cancellationToken);

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
        AgentSessionGeneration generation, string actor2, string role, string client, bool forceRebind,
        CancellationToken cancellationToken)
        => inner.RegisterAsync(generation, actor2, role, client, forceRebind, cancellationToken);
}

/// <summary>
/// Delegates every <see cref="IMailWakeBatchStore"/> member to
/// <paramref name="inner"/> except <see cref="TryRenewAsync"/>, which always
/// throws - simulating the renewal store call itself failing outright,
/// distinct from it merely returning false.
/// </summary>
internal sealed class ThrowingRenewMailWakeBatchStore(IMailWakeBatchStore inner) : IMailWakeBatchStore
{
    public Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId, string actor, string ownerId, string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryClaimAsync(nitroInstanceId, actor, ownerId, attemptId, targets, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated TryRenewAsync failure.");

    public Task<bool> TryCompleteAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryCompleteAsync(batchId, ownerId, attemptId, now, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, DateTimeOffset? retryAt,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(batchId, ownerId, attemptId, now, retryAt, lastError, cancellationToken);

    public Task<bool> TryRecordTargetOutcomeAsync(
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, string status,
        long? offeredGeneration, long? acceptedGeneration, string? lastError, DateTimeOffset now,
        CancellationToken cancellationToken)
        => inner.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, status, offeredGeneration, acceptedGeneration, lastError, now,
            cancellationToken);
}

/// <summary>
/// Delegates every <see cref="IMailWakeBatchStore"/> member to
/// <paramref name="inner"/> except <see cref="TryRecordTargetOutcomeAsync"/>:
/// its first call for <paramref name="racedTarget"/> first races in a
/// synthetic Delivered acceptance for the same target, through the same
/// owner/attempt fence the caller's own call is about to use, before
/// forwarding the caller's own (terminal-failure) call - simulating a
/// concurrent acceptance observation landing just before the terminal
/// outcome commits.
/// </summary>
internal sealed class RacingAcceptanceMailWakeBatchStoreDecorator(
    IMailWakeBatchStore inner, AgentSessionGeneration racedTarget, long acceptedGeneration) : IMailWakeBatchStore
{
    private bool _raced;

    public Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId, string actor, string ownerId, string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryClaimAsync(nitroInstanceId, actor, ownerId, attemptId, targets, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryRenewAsync(batchId, ownerId, attemptId, now, leaseDuration, cancellationToken);

    public Task<bool> TryCompleteAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryCompleteAsync(batchId, ownerId, attemptId, now, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, DateTimeOffset? retryAt,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(batchId, ownerId, attemptId, now, retryAt, lastError, cancellationToken);

    public async Task<bool> TryRecordTargetOutcomeAsync(
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, string status,
        long? offeredGeneration, long? acceptedGeneration_, string? lastError, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!_raced && target == racedTarget)
        {
            _raced = true;
            await inner.TryRecordTargetOutcomeAsync(
                batchId, target, ownerId, attemptId, MailWakeTargetStatus.Delivered,
                offeredGeneration: null, acceptedGeneration, lastError: null, now, cancellationToken);
        }

        return await inner.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, status, offeredGeneration, acceptedGeneration_, lastError, now,
            cancellationToken);
    }
}
