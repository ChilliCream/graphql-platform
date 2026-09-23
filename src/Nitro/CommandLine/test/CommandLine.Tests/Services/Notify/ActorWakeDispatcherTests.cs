using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Extensions.DependencyInjection;
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
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName));
        _globalConfigDirectoryProvider = new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName);
        _mail = new MailStore(
            _fileSystem, _timeProvider, _database,
            new AgentStore(_fileSystem, _timeProvider, _database), _instanceIdProvider,
            _globalConfigDirectoryProvider);
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
    public async Task DispatchAsync_Should_CompleteAsSkipped_When_TheActorHasNoLiveSession()
    {
        // arrange: mail enqueued, but the actor never claimed a live
        // session at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _agentRegistry.RegisterAsync(Actor, role: "", client: "", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Skipped, receipt.Status);
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
        // arrange
        // The session is gone by the time the target loop re-resolves it.
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
        // arrange
        // The decorator replaces the frozen session with the same session id on another host.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var frozen = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var rebound = frozen with { Host = "host-other" };
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

        // assert
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
        // arrange
        // A deadline 200ms out, inside WakeDispatchPolicy.HandoffObservationReserve (500ms).
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
        // arrange
        // A single live session whose transport call hangs until cancelled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = CreateDispatcher(hangingExecutor);

        // act
        // Wait for transport entry, then advance past the batch lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchLeaseDuration + TimeSpan.FromSeconds(5));

        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // An expired batch can be reclaimed by a fresh dispatch.
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
        // arrange
        // TryRenewAsync throws instead of returning false.
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

        // act
        // Wait for transport entry, then advance past the renew interval but not the lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchRenewInterval + TimeSpan.FromSeconds(1));

        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // Advance past the unrenewed lease before attempting a new dispatch.
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
        // arrange
        // Enqueue wake work for this instance ("host-1").
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

        // assert
        Assert.Null(receipt);

        var hostOneDispatcher = CreateDispatcher(new FakePingSessionExecutor());
        var hostOneReceipt = await hostOneDispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        Assert.NotNull(hostOneReceipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, hostOneReceipt.Status);
    }

    [Fact]
    public async Task DispatchAsync_Should_ReportFailed_When_ATerminalFailureRacesAConcurrentlyRecordedAcceptance()
    {
        // arrange
        // The decorator records Delivered immediately before forwarding the failure write.
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

        var boardGeneration = new AgentSessionGeneration(
            AgentSessionHarness.NitroBoard, "board-1", InstanceId);
        await _sessions.StartAsync(
            boardGeneration, "/work", "/work/.nitro/agents", AgentSessionEndpointKind.DbWatch, "local",
            envActor: "pascal", cancellationToken);

        var codexGeneration = new AgentSessionGeneration(
            AgentSessionHarness.Codex, "codex-session", InstanceId);
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
        var ledger = new SessionDeliveryLedger(_fileSystem, _database);
        var executor = new PingSessionExecutor(
            _mail, ledger, queueClient, new NoopClaudePeerClient(), _sessions, _leases, _timeProvider,
            new NoopOpencodeServerClient());
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
        using var document = System.Text.Json.JsonDocument.Parse(
            call.Message[(call.Message.IndexOf('\n') + 1)..]);
        var item = document.RootElement.GetProperty("items")[0];
        Assert.Equal(
            ("thread-1", message.Id, "check"),
            (call.ThreadId, item.GetProperty("id").GetString(), item.GetProperty("body").GetString()));
    }

    [Fact]
    public async Task DispatchAsync_Should_PersistLastPingAttemptAndResult_When_TheRealExecutorDispatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var queueClient = new FakeCodexQueueClient();
        var executor = new PingSessionExecutor(
            _mail, new SessionDeliveryLedger(_fileSystem, _database), queueClient, new NoopClaudePeerClient(),
            _sessions, _leases, _timeProvider, new NoopOpencodeServerClient());
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
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var row = await _sessions.FindByGenerationAsync(generation, cancellationToken);
        Assert.NotNull(row!.LastPingAttempt);
        Assert.Equal(AgentPingResult.Ok, row.LastPingResult);
    }

    [Fact]
    public async Task DispatchAsync_Should_PersistTheHealthOnlyResult_When_TheOpencodeSessionWakesTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new PingSessionExecutor(
            new NoUnreadMailStoreDecorator(_mail), new SessionDeliveryLedger(_fileSystem, _database),
            new FakeCodexQueueClient(), new NoopClaudePeerClient(), _sessions, _leases, _timeProvider,
            new NoopOpencodeServerClient());
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
        // The dispatcher sees unread mail; the executor's digest lookup returns none.
        var firstReceipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        var firstRow = await _sessions.FindByGenerationAsync(generation, cancellationToken);

        // assert
        Assert.NotNull(firstRow!.LastPingAttempt);
        Assert.Equal(
            (MailWakeTargetStatus.Delivered, AgentPingResult.Ok, PingSessionExecutor.HealthOnlyDetail),
            (firstReceipt?.Status, firstRow.LastPingResult, firstRow.LastPingDetail));

        // act
        // Rearm the idle-push gate, advance past cooldown, and enqueue another message.
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(cancellationToken);
        var secondReceipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        var secondRow = await _sessions.FindByGenerationAsync(generation, cancellationToken);

        // assert
        Assert.NotNull(secondRow!.LastPingAttempt);
        Assert.Equal(
            (MailWakeTargetStatus.Delivered, AgentPingResult.Ok, PingSessionExecutor.HealthOnlyDetail),
            (secondReceipt?.Status, secondRow.LastPingResult, secondRow.LastPingDetail));
    }

    [Fact]
    public async Task DispatchAsync_Should_PushTheDigestOnce_When_TheOpencodeSessionIsIdleArmed()
    {
        // arrange
        // RearmIdlePushAsync arms the idle-push gate.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var call = Assert.Single(executor.Calls);
        Assert.True(call.IsOpencodeServer);
    }

    [Fact]
    public async Task DispatchAsync_Should_SuppressTheSecondPush_When_TheOpencodeIdleTransitionWasNeverRearmed()
    {
        // arrange: a first dispatch already spent the one-shot idle-push
        // claim (see DispatchAsync_Should_PushTheDigestOnce_When_TheOpencodeSessionIsIdleArmed).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        await CreateDispatcher(new FakePingSessionExecutor()).DispatchAsync(Actor, Deadline(), cancellationToken);

        // act
        // Advance past the gate's cooldown, then dispatch a second wake with no fresh rearm.
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(cancellationToken);
        var suppressedExecutor = new FakePingSessionExecutor();
        var suppressedReceipt = await CreateDispatcher(suppressedExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert: suppressed rather than pushed again.
        Assert.NotNull(suppressedReceipt);
        var suppressedTarget = Assert.Single(suppressedReceipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, suppressedTarget.Status);
        Assert.Equal("idle-not-armed", suppressedTarget.LastError);
        Assert.Empty(suppressedExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_OfferTheTarget_When_TheOpencodeSessionIsNotIdleArmed()
    {
        // arrange
        // A session whose idle-push gate was never armed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert: offered, not failed, and no transport was ever attempted.
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);
        Assert.Equal("idle-not-armed", target.LastError);
        Assert.Empty(executor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_DeliverTheOfferedTarget_When_TheIdleTransitionIsLaterRearmed()
    {
        // arrange
        // A first dispatch already offered the target as idle-not-armed.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        await CreateDispatcher(new FakePingSessionExecutor()).DispatchAsync(Actor, Deadline(), cancellationToken);

        // act: once a genuine prompt rearms the gate and the retry becomes
        // due, the same still-unread mail is delivered.
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        var rearmedExecutor = new FakePingSessionExecutor();
        var rearmedReceipt = await CreateDispatcher(rearmedExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(rearmedReceipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, rearmedReceipt.Status);
        Assert.Single(rearmedExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_LeaveTheIdlePushClaimArmed_When_TheOpencodeSessionGateWasBusy()
    {
        // arrange
        // The session ping gate is held by an unrelated attempt.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        await _gates.TryAcquireAsync(
            generation, "external-holder", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var busyExecutor = new FakePingSessionExecutor();

        // act: the gate is busy, so the target is offered rather than
        // failed, and no transport is ever attempted.
        var busyReceipt = await CreateDispatcher(busyExecutor).DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal("busy", Assert.Single(busyReceipt!.Targets).LastError);
        Assert.Empty(busyExecutor.Calls);

        // act
        // Release the gate and let the offered retry become due.
        await _gates.ReleaseAsync(generation, "external-holder", cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, deliveredReceipt?.Status);
        Assert.Single(deliveredExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_RearmTheIdlePushClaim_When_TheOpencodeTransportFailsOutright()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var failingExecutor = new FakePingSessionExecutor { NextReason = PingAttemptReason.EndpointGone };

        // act: the transport fails outright.
        var failedReceipt = await CreateDispatcher(failingExecutor).DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Failed, failedReceipt?.Status);

        // act
        // New mail arrives after the failed attempt already rearmed the claim.
        await SendEnqueuedMailAsync(cancellationToken);
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, deliveredReceipt?.Status);
        Assert.Single(deliveredExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_RearmTheIdlePushClaim_When_TheOpencodeAttemptWasHealthOnly()
    {
        // arrange
        // The executor reports a successful ping that pushed nothing (PingSessionExecutor.HealthOnlyDetail).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var healthOnlyExecutor = new FakePingSessionExecutor { NextDetail = PingSessionExecutor.HealthOnlyDetail };

        // act
        var healthOnlyReceipt = await CreateDispatcher(healthOnlyExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, healthOnlyReceipt?.Status);

        // act
        // Advance past the gate's cooldown, then dispatch again with the rearmed claim.
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(cancellationToken);
        var pushExecutor = new FakePingSessionExecutor();
        var pushReceipt = await CreateDispatcher(pushExecutor).DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, pushReceipt?.Status);
        Assert.Single(pushExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_RearmTheIdlePushClaim_When_TheOpencodeDispatchIsCancelledMidTransport()
    {
        // arrange
        // The idle-push claim is spent, then the transport call hangs.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _sessions.RearmIdlePushAsync(generation, cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = CreateDispatcher(hangingExecutor);

        // act
        // Wait for transport entry, then advance past the batch lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(Actor, Deadline(), cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchLeaseDuration + TimeSpan.FromSeconds(5));
        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // act
        // New mail arrives after the aborted attempt already rearmed the claim.
        await SendEnqueuedMailAsync(cancellationToken);
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(Actor, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, deliveredReceipt?.Status);
        Assert.Single(deliveredExecutor.Calls);
    }

    [Fact]
    public void AddNitroServices_Should_ResolveActorWakeDispatcher_When_BuiltFromTheServiceCollection()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddNitroServices();

        // act
        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IActorWakeDispatcher>();

        // assert
        Assert.IsType<ActorWakeDispatcher>(dispatcher);
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
        // Start a fresh session on the current Nitro instance.
        var harness = endpointKind switch
        {
            AgentSessionEndpointKind.ClaudePeer => AgentSessionHarness.ClaudeCode,
            AgentSessionEndpointKind.OpencodeServer => AgentSessionHarness.Opencode,
            _ => AgentSessionHarness.Codex
        };
        var generation = new AgentSessionGeneration(harness, sessionId, InstanceId);

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
