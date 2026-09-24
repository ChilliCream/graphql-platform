using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Hook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="ActorWakeDispatcher"/> against a real workspace
/// database, with a scriptable <see cref="FakePingSessionExecutor"/>
/// standing in for the real transports: the claim/no-outstanding-work
/// branch, the offline and mail-already-read fast paths, per-target failure
/// mapping (unsupported, terminal transport failure), the access-denied
/// offer-and-retain-siblings contract, and losing the batch's own lease
/// renewal mid-dispatch (cancelling an in-flight target without asserting
/// its outcome) followed by a clean reclaim on the next dispatch.
/// </summary>
public sealed class ActorWakeDispatcherTests : IDisposable
{
    private const string LeaderToken = "leader-token";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentDatabase _database;
    private readonly AgentStore _agentStore;
    private readonly AgentRegistry _agentRegistry;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly AgentPingGateStore _gates;
    private readonly PingLeaseStore _leases;
    private readonly SessionGateCoordinator _gateCoordinator;
    private readonly MailWakeDaemonLeaderStore _leaderStore;

    public ActorWakeDispatcherTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-actor-wake-dispatcher-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _database = new AgentDatabase();
        _agentStore = new AgentStore(_fileSystem, _timeProvider, _database);
        _agentRegistry = new AgentRegistry(_fileSystem, _timeProvider, _database);
        _mail = new MailStore(_fileSystem, _timeProvider, _database, _agentStore);
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        _gates = new AgentPingGateStore(_fileSystem, _database);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _gateCoordinator = new SessionGateCoordinator(_gates, _leases);
        _leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task DispatchAsync_Should_ReturnNull_When_NothingIsOutstanding()
    {
        // arrange
        // No mail was ever sent, so the actor has no mail_wake_outbox row at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var dispatcher = CreateDispatcher(new FakePingSessionExecutor());

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.Null(receipt);
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipWithOfflineReason_When_TheAgentIsPastTheOnlineWindow()
    {
        // arrange
        // A session-bound agent, advanced well past the online window.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        _timeProvider.Advance(AgentStateResolver.OnlineWindow + TimeSpan.FromMinutes(1));
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(
            (MailWakeTargetStatus.Skipped, actor, MailWakeTargetStatus.Skipped, "offline"),
            (receipt.Status, target.Target, target.Status, target.LastError));
        Assert.Empty(executor.Calls);

        // the batch completed instead of releasing for a retry.
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        Assert.Null(await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipWithEndedReason_When_TheAgentHasEnded()
    {
        // arrange
        // A session-bound agent whose harness session has ended.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await _agentStore.EndSessionAsync(AgentSessionHarness.Codex, "session-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(
            (MailWakeTargetStatus.Skipped, actor, MailWakeTargetStatus.Skipped, "ended"),
            (receipt.Status, target.Target, target.Status, target.LastError));
        Assert.Empty(executor.Calls);

        // the batch completed instead of releasing for a retry.
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        Assert.Null(await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipWithUnreachableReason_When_TheAgentIsLoginOnly()
    {
        // arrange
        // A login-only agent, still within the online window but with no endpoint.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = (await _agentStore.LoginAsync(cancellationToken)).Name;
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(
            (MailWakeTargetStatus.Skipped, actor, MailWakeTargetStatus.Skipped, "unreachable"),
            (receipt.Status, target.Target, target.Status, target.LastError));
        Assert.Empty(executor.Calls);

        // the batch completed instead of releasing for a retry.
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        Assert.Null(await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipWithUnreachableReason_When_TheSessionIdIsMissingOnAClaudePeerEndpoint()
    {
        // arrange
        // A login-only agent with a claude-peer endpoint but no session id.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = (await _agentStore.LoginAsync(cancellationToken)).Name;
        await _agentStore.SetEndpointAsync(
            actor, AgentSessionEndpointKind.ClaudePeer, "peer-addr", null, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(
            (MailWakeTargetStatus.Skipped, actor, MailWakeTargetStatus.Skipped, "unreachable"),
            (receipt.Status, target.Target, target.Status, target.LastError));
        Assert.Empty(executor.Calls);

        // the batch completed instead of releasing for a retry.
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        Assert.Null(await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_Should_SkipWithOfflineReason_When_TheAgentRowIsMissing()
    {
        // arrange
        // The batch claims a target whose agent row no longer exists.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var missingStore = new Mock<IAgentStore>();
        missingStore
            .Setup(store => store.FindAsync(actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentRow?)null);
        var dispatcher = new ActorWakeDispatcher(
            _batches, missingStore.Object, _gateCoordinator, executor, _mail, _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(
            (MailWakeTargetStatus.Skipped, actor, MailWakeTargetStatus.Skipped, "offline"),
            (receipt.Status, target.Target, target.Status, target.LastError));
        Assert.Empty(executor.Calls);

        // the batch completed instead of releasing for a retry.
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        Assert.Null(await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_Should_SatisfyEveryTarget_When_TheMailWasAlreadyReadBeforeDispatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        var message = await SendEnqueuedMailAsync(actor, cancellationToken);
        await _mail.MarkReadAsync([message.Id], actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        // Satisfied, zero, no transport attempted, and no second message row written.
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Satisfied, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(actor, target.Target);
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
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(actor, target.Target);
        Assert.Equal(MailWakeTargetStatus.Delivered, target.Status);
        Assert.Single(executor.Calls);

        // the batch settled: nothing left outstanding for a fresh dispatch.
        var again = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
        Assert.Null(again);
    }

    [Fact]
    public async Task DispatchAsync_Should_RecordFailed_When_TheTransportEndsInAnUnacceptedTerminalFailure()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor { NextReason = PingAttemptReason.Timeout };
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal(PingAttemptReason.Timeout.ToString(), target.LastError);
    }

    [Fact]
    public async Task DispatchAsync_Should_ClampTheAttemptDeadline_When_TheSharedDeadlineIsAlreadyWithinTheHandoffReserve()
    {
        // arrange
        // A deadline 200ms out, inside WakeDispatchPolicy.HandoffObservationReserve (500ms).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);
        var tightDeadline = _timeProvider.GetUtcNow() + TimeSpan.FromMilliseconds(200);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, tightDeadline, cancellationToken);

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
        // A single live agent whose transport call hangs until cancelled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = CreateDispatcher(hangingExecutor);

        // act
        // Wait for transport entry, then advance past the batch lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
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
        var reclaimed = await freshDispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(MailWakeTargetStatus.Delivered, reclaimed.Status);
        Assert.Equal(actor, Assert.Single(reclaimed.Targets).Target);
    }

    [Fact]
    public async Task DispatchAsync_Should_AbandonInFlightTargets_When_TheBatchRenewalThrows_And_AllowReclaimOnceTheLeaseExpires()
    {
        // arrange
        // TryRenewAsync throws instead of returning false.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = new ActorWakeDispatcher(
            new ThrowingRenewMailWakeBatchStore(_batches),
            _agentStore,
            _gateCoordinator,
            hangingExecutor,
            _mail,
            _timeProvider);

        // act
        // Wait for transport entry, then advance past the renew interval but not the lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
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
        var reclaimed = await freshDispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        Assert.NotNull(reclaimed);
        Assert.Equal(MailWakeTargetStatus.Delivered, reclaimed.Status);
        Assert.Equal(actor, Assert.Single(reclaimed.Targets).Target);
    }

    [Fact]
    public async Task DispatchAsync_Should_ReportFailed_When_ATerminalFailureRacesAConcurrentlyRecordedAcceptance()
    {
        // arrange
        // The decorator records Delivered immediately before forwarding the failure write.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor { NextReason = PingAttemptReason.Timeout };
        var racingBatches = new RacingAcceptanceMailWakeBatchStoreDecorator(_batches, actor, acceptedGeneration: 1);
        var dispatcher = new ActorWakeDispatcher(
            racingBatches,
            _agentStore,
            _gateCoordinator,
            executor,
            _mail,
            _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Failed, receipt.Status);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Failed, target.Status);
        Assert.Equal(PingAttemptReason.Timeout.ToString(), target.LastError);

        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM mail_wake_targets WHERE agent = @agent";
        command.Parameters.AddWithValue("@agent", actor);
        var status = (string)(await command.ExecuteScalarAsync(cancellationToken))!;
        Assert.Equal(MailWakeTargetStatus.Failed, status);
    }

    [Fact]
    public async Task DispatchAsync_Should_PersistLastPingAttemptAndResult_When_TheRealExecutorDispatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var queueClient = new FakeCodexQueueClient();
        var executor = new PingSessionExecutor(
            _mail, new AgentDeliveryLedger(_fileSystem, _database), queueClient, new NoopClaudePeerClient(),
            _agentStore, _leases, _timeProvider, new NoopOpencodeServerClient());
        var dispatcher = new ActorWakeDispatcher(
            _batches,
            _agentStore,
            _gateCoordinator,
            executor,
            _mail,
            _timeProvider);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var row = await _agentStore.FindAsync(actor, cancellationToken);
        Assert.NotNull(row!.LastPingAttempt);
        Assert.Equal(AgentPingResult.Ok, row.LastPingResult);
    }

    [Fact]
    public async Task DispatchAsync_Should_PersistTheHealthOnlyResult_When_TheOpencodeSessionWakesTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new PingSessionExecutor(
            new NoUnreadMailStoreDecorator(_mail), new AgentDeliveryLedger(_fileSystem, _database),
            new FakeCodexQueueClient(), new NoopClaudePeerClient(), _agentStore, _leases, _timeProvider,
            new NoopOpencodeServerClient());
        var dispatcher = new ActorWakeDispatcher(
            _batches,
            _agentStore,
            _gateCoordinator,
            executor,
            _mail,
            _timeProvider);

        // act
        // The dispatcher sees unread mail; the executor's digest lookup returns none.
        var firstReceipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
        var firstRow = await _agentStore.FindAsync(actor, cancellationToken);

        // assert
        Assert.NotNull(firstRow!.LastPingAttempt);
        Assert.Equal(
            (MailWakeTargetStatus.Delivered, AgentPingResult.Ok, PingSessionExecutor.HealthOnlyDetail),
            (firstReceipt?.Status, firstRow.LastPingResult, firstRow.LastPingDetail));

        // act
        // Rearm the idle-push gate, advance past cooldown, and enqueue another message.
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var secondReceipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
        var secondRow = await _agentStore.FindAsync(actor, cancellationToken);

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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, receipt.Status);
        var call = Assert.Single(executor.Calls);
        Assert.True(call.IsOpencodeServer);
    }

    [Fact]
    public async Task DispatchAsync_Should_SuppressTheSecondPush_When_TheOpencodeIdleTransitionWasNeverRearmed()
    {
        // arrange
        // A first dispatch already spent the one-shot idle-push claim.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        await CreateDispatcher(new FakePingSessionExecutor()).DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // act
        // Advance past the gate's cooldown, then dispatch a second wake with no fresh rearm.
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var suppressedExecutor = new FakePingSessionExecutor();
        var suppressedReceipt = await CreateDispatcher(suppressedExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        // suppressed rather than pushed again.
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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var executor = new FakePingSessionExecutor();
        var dispatcher = CreateDispatcher(executor);

        // act
        var receipt = await dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        // offered, not failed, and no transport was ever attempted.
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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        await CreateDispatcher(new FakePingSessionExecutor()).DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // act
        // A genuine prompt rearms the gate, and the retry becomes due.
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        var rearmedExecutor = new FakePingSessionExecutor();
        var rearmedReceipt = await CreateDispatcher(rearmedExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.NotNull(rearmedReceipt);
        Assert.Equal(MailWakeTargetStatus.Delivered, rearmedReceipt.Status);
        Assert.Single(rearmedExecutor.Calls);
    }

    [Fact]
    public async Task DispatchAsync_Should_LeaveTheIdlePushClaimArmed_When_TheOpencodeSessionGateWasBusy()
    {
        // arrange
        // The agent's ping gate is held by an unrelated attempt.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        await _gates.TryAcquireAsync(
            actor, "external-holder", _timeProvider.GetUtcNow(), TimeSpan.FromSeconds(30), cancellationToken);
        var busyExecutor = new FakePingSessionExecutor();

        // act
        // The gate is busy, so the target is offered rather than failed.
        var busyReceipt = await CreateDispatcher(busyExecutor).DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.Equal("busy", Assert.Single(busyReceipt!.Targets).LastError);
        Assert.Empty(busyExecutor.Calls);

        // act
        // Release the gate and let the offered retry become due.
        await _gates.ReleaseAsync(actor, "external-holder", cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.OfferedRetryDelay + TimeSpan.FromSeconds(1));
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var failingExecutor = new FakePingSessionExecutor { NextReason = PingAttemptReason.EndpointGone };

        // act
        // the transport fails outright.
        var failedReceipt = await CreateDispatcher(failingExecutor).DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Failed, failedReceipt?.Status);

        // act
        // New mail arrives after the failed attempt already rearmed the claim.
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var healthOnlyExecutor = new FakePingSessionExecutor { NextDetail = PingSessionExecutor.HealthOnlyDetail };

        // act
        var healthOnlyReceipt = await CreateDispatcher(healthOnlyExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

        // assert
        Assert.Equal(MailWakeTargetStatus.Delivered, healthOnlyReceipt?.Status);

        // act
        // Advance past the gate's cooldown, then dispatch again with the rearmed claim.
        _timeProvider.Advance(PingPolicy.Cooldown + TimeSpan.FromSeconds(1));
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var pushExecutor = new FakePingSessionExecutor();
        var pushReceipt = await CreateDispatcher(pushExecutor).DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

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
        var actor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.OpencodeServer, "http://127.0.0.1:4096", cancellationToken);
        await _agentStore.RearmIdlePushAsync(actor, cancellationToken);
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var hangingExecutor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var dispatcher = CreateDispatcher(hangingExecutor);

        // act
        // Wait for transport entry, then advance past the batch lease expiry.
        var dispatchTask = dispatcher.DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);
        await hangingExecutor.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        _timeProvider.Advance(WakeDispatchPolicy.BatchLeaseDuration + TimeSpan.FromSeconds(5));
        var receipt = await dispatchTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.NotNull(receipt);
        var target = Assert.Single(receipt.Targets);
        Assert.Equal(MailWakeTargetStatus.Pending, target.Status);

        // act
        // New mail arrives after the aborted attempt already rearmed the claim.
        await SendEnqueuedMailAsync(actor, cancellationToken);
        var deliveredExecutor = new FakePingSessionExecutor();
        var deliveredReceipt = await CreateDispatcher(deliveredExecutor)
            .DispatchAsync(actor, LeaderToken, Deadline(), cancellationToken);

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
        => new(_batches, _agentStore, _gateCoordinator, executor, _mail, _timeProvider);

    /// <summary>
    /// Initializes the workspace database and acquires the fixed leader
    /// lease every dispatch in this class fences its batch writes on.
    /// </summary>
    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }

        await _leaderStore.TryAcquireAsync(
            LeaderToken, _timeProvider.GetUtcNow(), TimeSpan.FromDays(1), cancellationToken);
    }

    /// <summary>
    /// Mints an agent row with a live harness session and endpoint,
    /// returning its allocated name.
    /// </summary>
    private async Task<string> SeedLiveSessionAsync(
        string endpointKind,
        string endpointAddr,
        CancellationToken cancellationToken,
        string sessionId = "session-1")
    {
        var harness = endpointKind switch
        {
            AgentSessionEndpointKind.ClaudePeer => AgentSessionHarness.ClaudeCode,
            AgentSessionEndpointKind.OpencodeServer => AgentSessionHarness.Opencode,
            _ => AgentSessionHarness.Codex
        };

        var result = await _agentStore.StartSessionAsync(
            new AgentSessionStartRequest
            {
                Harness = harness,
                SessionId = sessionId,
                HarnessVersion = "1.0.0",
                Cwd = "/work",
                WorkspacePath = "/work/.nitro/agents",
                EndpointKind = endpointKind,
                EndpointAddr = endpointAddr
            },
            cancellationToken);

        return result.Row!.Name;
    }

    private async Task<MailMessage> SendEnqueuedMailAsync(string actor, CancellationToken cancellationToken)
    {
        // Registers the mail sender behind the store's sender-usability check.
        await _agentRegistry.RegisterAsync("pascal", role: "", client: "", cancellationToken);

        return await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "pascal",
                Subject = "status",
                Body = "check",
                To = [actor],
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);
    }
}
