using System.Collections.Concurrent;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeDaemonCoordinator"/> against a real workspace
/// database: leadership acquisition and handoff, the admission and execution
/// loops, self-degradation, a graceful stop, and demotion on a lost or fenced lease.
/// </summary>
public sealed class MailWakeDaemonCoordinatorTests : IDisposable
{
    private const string Actor = "codex-worker";

    private static readonly MailWakeDaemonPolicy s_fastPolicy = new(
        LeaderLeaseDuration: TimeSpan.FromSeconds(10),
        HeartbeatInterval: TimeSpan.FromMilliseconds(60),
        AdmissionPollInterval: TimeSpan.FromMilliseconds(30),
        StandbyPollInterval: TimeSpan.FromMilliseconds(30),
        MaxConcurrentActorExecutions: 4,
        ShutdownWait: TimeSpan.FromSeconds(2));

    private static readonly TimeSpan s_waitTimeout = TimeSpan.FromSeconds(5);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentStore _agentStore;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly SessionGateCoordinator _gateCoordinator;

    public MailWakeDaemonCoordinatorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-daemon-coordinator-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, TimeProvider.System, _database);
        _agentStore = new AgentStore(_fileSystem, TimeProvider.System, _database);
        _mail = new MailStore(_fileSystem, TimeProvider.System, _database, _agentStore);
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        var gates = new AgentPingGateStore(_fileSystem, _database);
        var leases = new PingLeaseStore(_fileSystem, _database);
        _gateCoordinator = new SessionGateCoordinator(gates, leases);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task StartAsync_Should_BecomeReady_When_NoOtherLeaderExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert
        Assert.NotNull(coordinator.Status.OwnerToken);
        Assert.Equal(await ReadLeaderOwnerTokenAsync(cancellationToken), coordinator.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_StayStandby_When_AnotherOwnerAlreadyHoldsALiveLease()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        await leaderStore.TryAcquireAsync("other-owner", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30), cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinator.StartAsync(cancellationToken);
        await Task.Delay(s_fastPolicy.StandbyPollInterval * 5, cancellationToken);

        // assert
        // never became ready while the other owner's lease is live.
        Assert.Equal(MailWakeDaemonState.Standby, coordinator.Status.State);
        Assert.Null(coordinator.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StandbyCoordinator_Should_AcquireLeadership_When_ThePriorLeaseExpires()
    {
        // arrange
        // A short-lived lease held by a different owner, acquired directly through the store.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var firstAcquired = await leaderStore.TryAcquireAsync(
            "other-owner", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert
        // took over with its own token once the prior lease expired.
        Assert.True(firstAcquired);
        Assert.NotNull(coordinator.Status.OwnerToken);
        Assert.NotEqual("other-owner", coordinator.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DispatchOutstandingActorWork_Through_TheAdmissionAndExecutionLoops()
    {
        // arrange
        // Enqueue mail for a live agent without calling the dispatcher directly.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor();
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => !executor.Calls.IsEmpty, cancellationToken);
        await WaitUntilAsync(
            async () => await ReadTargetStatusAsync(actor, cancellationToken) == MailWakeTargetStatus.Delivered,
            cancellationToken);

        // assert
        Assert.Single(executor.Calls);
        Assert.Equal(MailWakeDaemonState.Ready, coordinator.Status.State);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DegradeAndReleaseLeadership_When_ItsOwnDispatchIsAccessDenied()
    {
        // arrange
        // The only live target denies Claude socket access.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor();
        executor.ReasonByActor[actor] = PingAttemptReason.AccessDenied;
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Degraded, cancellationToken);

        // assert
        // Degraded with the denial recorded, and it does not flap back to ready.
        Assert.Equal("access-denied", coordinator.Status.LastError);
        await Task.Delay(s_fastPolicy.StandbyPollInterval * 5, cancellationToken);
        Assert.NotEqual(MailWakeDaemonState.Ready, coordinator.Status.State);

        // a differently privileged standby (a second coordinator instance)
        // can take over immediately, without waiting out the lease.
        await using var standby = CreateCoordinator(new FakePingSessionExecutor());
        await standby.StartAsync(cancellationToken);
        await WaitUntilAsync(() => standby.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        Assert.NotNull(standby.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
        await standby.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StopAsync_Should_ReturnWithinTheShutdownBudget_And_ReleaseLeadershipImmediately()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await coordinator.StopAsync(cancellationToken);
        stopwatch.Stop();

        // assert
        Assert.True(stopwatch.Elapsed < s_fastPolicy.ShutdownWait, $"StopAsync took {stopwatch.Elapsed}.");
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var reacquired = await leaderStore.TryAcquireAsync(
            "someone-else", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.True(reacquired);
    }

    [Fact]
    public async Task StartAsync_Should_RecoverAndBecomeReady_When_TheLeaderStoreFaultsOnce()
    {
        // arrange
        // The leader store throws a non-busy exception on the first acquire attempt.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var faultingStore = new FaultingLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), faultingStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.LastError == "readonly", cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert
        Assert.Equal(MailWakeDaemonState.Ready, coordinator.Status.State);
        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DemoteToStandby_And_CancelInFlightDispatch_When_RenewalIsLost()
    {
        // arrange
        // A live agent with a hanging transport call, then the next heartbeat renewal is made to fail.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var renewalLossStore = new RenewalLossLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        await using var coordinator = CreateCoordinator(executor, renewalLossStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await executor.Entered.Task.WaitAsync(s_waitTimeout, cancellationToken);
        renewalLossStore.FailNextRenewal();
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Standby, cancellationToken);

        // assert
        // Demoted, and the hung dispatch never recorded a delivery.
        var status = await ReadTargetStatusAsync(actor, cancellationToken);
        Assert.NotEqual(MailWakeTargetStatus.Delivered, status);
        Assert.Null(coordinator.Status.OwnerToken);
        Assert.Null(coordinator.Status.LeaseExpiresAt);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DemoteToStandby_When_ASecondCoordinatorAcquiresItsExpiredLease()
    {
        // arrange
        // A gated leader store holds coordinator A's renewal in flight while B claims the row it already lost.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sharedLeaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var gatedLeaderStore = new GatedRenewalLeaderStore(sharedLeaderStore);
        var executorA = new FakePingSessionExecutor { HangUntilCancelled = true };
        await using var coordinatorA = new MailWakeDaemonCoordinator(
            gatedLeaderStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, executorA, _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy);
        await using var coordinatorB = new MailWakeDaemonCoordinator(
            sharedLeaderStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, new FakePingSessionExecutor(), _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy);

        // act
        await coordinatorA.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinatorA.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        await executorA.Entered.Task.WaitAsync(s_waitTimeout, cancellationToken);
        var staleToken = coordinatorA.Status.OwnerToken!;
        gatedLeaderStore.HoldNextRenewal();
        timeProvider.Advance(s_fastPolicy.LeaderLeaseDuration + s_fastPolicy.HeartbeatInterval);
        await WaitUntilAsync(() => coordinatorA.Status.State == MailWakeDaemonState.Standby, cancellationToken);
        var aStandbyWhileRenewalHeld = coordinatorA.Status.State;

        await coordinatorB.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinatorB.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        gatedLeaderStore.ReleaseHeldRenewal();
        await gatedLeaderStore.RenewalCompleted.WaitAsync(s_waitTimeout, cancellationToken);
        var (batchId, attemptId) = await ReadActiveBatchAsync(actor, cancellationToken);
        var staleOutcomeAccepted = await _batches.TryRecordTargetOutcomeAsync(
            batchId, actor, staleToken, attemptId, MailWakeTargetStatus.Delivered,
            offeredGeneration: null, acceptedGeneration: 1, lastError: null, timeProvider.GetUtcNow(), cancellationToken);
        var staleCompleteAccepted = await _batches.TryCompleteAsync(
            batchId, staleToken, attemptId, timeProvider.GetUtcNow(), cancellationToken);

        // assert
        Assert.Equal(MailWakeDaemonState.Standby, aStandbyWhileRenewalHeld);
        var finalState = (
            AState: coordinatorA.Status.State,
            AOwnerToken: coordinatorA.Status.OwnerToken,
            ALeaseExpiresAt: coordinatorA.Status.LeaseExpiresAt,
            RowOwnerToken: await ReadLeaderOwnerTokenAsync(cancellationToken),
            StaleOutcomeAccepted: staleOutcomeAccepted,
            StaleCompleteAccepted: staleCompleteAccepted,
            TargetStatus: await ReadTargetStatusAsync(actor, cancellationToken));
        Assert.Equal(
            (MailWakeDaemonState.Standby, null, null, coordinatorB.Status.OwnerToken,
                false, false, MailWakeTargetStatus.Pending),
            finalState);

        await coordinatorA.StopAsync(cancellationToken);
        await coordinatorB.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task AdmissionLoop_Should_StopDispatching_When_TheCachedLeaderLeaseHasLapsed()
    {
        // arrange
        // A gated leader store holds the heartbeat's renewal in flight while the cached lease goes stale.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var gatedLeaderStore = new GatedRenewalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var dispatcher = new CountingDispatcher();
        await using var coordinator = new MailWakeDaemonCoordinator(
            gatedLeaderStore, dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        gatedLeaderStore.HoldNextRenewal();
        timeProvider.Advance(s_fastPolicy.LeaderLeaseDuration + s_fastPolicy.HeartbeatInterval);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, Actor);
        timeProvider.Advance(s_fastPolicy.AdmissionPollInterval);
        await Task.Delay(s_fastPolicy.AdmissionPollInterval * 5, cancellationToken);

        // assert
        // The lapsed cached lease stopped admission before the heartbeat's held renewal ever returned.
        Assert.Equal(0, dispatcher.DispatchCount);
        Assert.Equal(MailWakeDaemonState.Standby, coordinator.Status.State);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_AdmitASecondActor_While_TheFirstActorsTransportIsBlocked()
    {
        // arrange
        // Two actors with enqueued mail, and every transport call hangs until cancelled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var firstActor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken, sessionId: "session-1");
        var secondActor = await SeedLiveSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-2", cancellationToken, sessionId: "session-2");
        await SendEnqueuedMailAsync(cancellationToken, firstActor);
        await SendEnqueuedMailAsync(cancellationToken, secondActor);
        var executor = new FakePingSessionExecutor { HangUntilCancelled = true };
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(500);

        while (executor.Calls.Select(c => c.ActorName).Distinct().Count() < 2)
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("The second actor was not admitted within 500 ms.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
        }

        // assert
        // Both actors were admitted concurrently, while neither transport had completed.
        Assert.Equal(2, executor.Calls.Select(c => c.ActorName).Distinct().Count());

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_BecomeReady_When_TheLeaderStoreIsBusyTwice()
    {
        // arrange
        // The leader store throws SQLITE_BUSY on the first two acquire attempts.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var busyStore = new BusyLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), busyAcquireCalls: 2);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), busyStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert
        // Retried through both busy attempts and became ready on the third.
        Assert.Equal(3, busyStore.AcquireCalls);
        Assert.Null(coordinator.Status.LastError);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_GiveUpTheTick_And_RetryOnTheNextStandbyPoll_When_BusyRetriesAreExhausted()
    {
        // arrange
        // The leader store throws SQLITE_BUSY on the first five acquire attempts, then succeeds.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var busyStore = new BusyLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), busyAcquireCalls: 5);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), busyStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(
            () => coordinator.Status.State == MailWakeDaemonState.Ready,
            cancellationToken,
            timeout: TimeSpan.FromSeconds(10));

        // assert
        // Five attempts exhausted the first tick, and a sixth succeeded on the next poll.
        Assert.Equal(6, busyStore.AcquireCalls);
        Assert.Null(coordinator.Status.LastError);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_APriorStopTimedOut_And_TheRunLoopIsStillAlive()
    {
        // arrange
        // The only outstanding actor's dispatch hangs forever and ignores cancellation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, Actor);
        var shortShutdownPolicy = s_fastPolicy with { ShutdownWait = TimeSpan.FromMilliseconds(50) };
        var dispatcher = new HangingDispatcher();
        var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            dispatcher,
            _fileSystem,
            _database,
            TimeProvider.System,
            shortShutdownPolicy);
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        await WaitUntilAsync(() => dispatcher.EnteredCount > 0, cancellationToken);
        await coordinator.StopAsync(cancellationToken);

        // act & assert: a still-alive orphaned run loop keeps StartAsync's
        // guard throwing for this instance.
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartAsync(cancellationToken));

        // Release the dispatch and wait for leadership cleanup before disposing the coordinator.
        var leaseExpiresBeforeRelease = await ReadLeaderExpiresAtAsync(cancellationToken);
        dispatcher.Release();
        await WaitUntilAsync(
            async () => await ReadLeaderExpiresAtAsync(cancellationToken) != leaseExpiresBeforeRelease,
            cancellationToken);
        await coordinator.DisposeAsync();
    }

    [Fact]
    public async Task RunningLeader_Should_ReleaseLeadership_And_CancelSiblings_When_AccessDeniedReleaseIsBusy()
    {
        // arrange
        // One actor is denied access, a second hangs on transport, and release throws SQLITE_BUSY once.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        const string deniedActor = "denied-actor";
        const string hungActor = "hung-actor";
        await _agentRegistry.EnsureImplicitAsync(deniedActor, cancellationToken);
        await _agentRegistry.EnsureImplicitAsync(hungActor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, deniedActor);
        await InsertDueOutboxRowAsync(cancellationToken, hungActor);
        var events = new ConcurrentQueue<string>();
        var dispatcher = new DeniedThenHangingDispatcher(deniedActor, events);
        var busyReleaseStore = new BusyReleaseLeaderStore(
            new MailWakeDaemonLeaderStore(_fileSystem, _database), busyReleaseCalls: 1, events);
        await using var coordinator = new MailWakeDaemonCoordinator(
            busyReleaseStore,
            dispatcher,
            _fileSystem,
            _database,
            TimeProvider.System,
            s_fastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Degraded, cancellationToken);

        // the busy-retried release only completes ~250ms (the policy's
        // initial backoff) after the first, busy attempt, so wait for the
        // actual retry rather than for Degraded alone.
        await WaitUntilAsync(() => busyReleaseStore.ReleaseCalls >= 2, cancellationToken);

        // assert
        Assert.Equal(2, busyReleaseStore.ReleaseCalls);
        var ordered = events.ToArray();
        var cancelledIndex = Array.IndexOf(ordered, $"{hungActor}-cancelled");
        var releasedIndex = Array.IndexOf(ordered, "release-attempted");
        Assert.True(
            cancelledIndex >= 0 && releasedIndex >= 0 && cancelledIndex < releasedIndex,
            $"Expected \"{hungActor}-cancelled\" before \"release-attempted\". Events: [{string.Join(", ", ordered)}]");

        // a differently privileged standby can take over immediately,
        // proving the release actually reached the database rather than
        // leaving this instance wedged as leader.
        await using var standby = CreateCoordinator(new FakePingSessionExecutor());
        await standby.StartAsync(cancellationToken);
        await WaitUntilAsync(() => standby.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        Assert.NotNull(standby.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
        await standby.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_NeverExceedMaxConcurrentActorExecutions_When_MoreActorsAreDueThanTheLimit()
    {
        // arrange
        // Five due actors under a policy capped at four concurrent executions.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actors = Enumerable.Range(1, 5).Select(i => $"actor-{i}").ToArray();

        foreach (var actor in actors)
        {
            await _agentRegistry.EnsureImplicitAsync(actor, cancellationToken);
            await InsertDueOutboxRowAsync(cancellationToken, actor);
        }

        var dispatcher = new ConcurrencyTrackingDispatcher();
        await using var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            dispatcher,
            _fileSystem,
            _database,
            TimeProvider.System,
            s_fastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => dispatcher.CompletedActors.Distinct().Count() >= actors.Length, cancellationToken);

        // assert
        // The gate's capacity was reached, and never exceeded, while draining all five actors.
        Assert.Equal(s_fastPolicy.MaxConcurrentActorExecutions, dispatcher.MaxObservedConcurrency);

        await coordinator.StopAsync(cancellationToken);
    }

    private MailWakeDaemonCoordinator CreateCoordinator(
        FakePingSessionExecutor executor, IMailWakeDaemonLeaderStore? leaderStore = null)
        => new(
            leaderStore ?? new MailWakeDaemonLeaderStore(_fileSystem, _database),
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, executor, _mail, TimeProvider.System),
            _fileSystem,
            _database,
            TimeProvider.System,
            s_fastPolicy);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    /// <summary>
    /// Mints an agent row with a live harness session and endpoint,
    /// returning its allocated name.
    /// </summary>
    private async Task<string> SeedLiveSessionAsync(
        string endpointKind, string endpointAddr, CancellationToken cancellationToken, string sessionId = "session-1")
    {
        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;

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

    private async Task<MailMessage> SendEnqueuedMailAsync(CancellationToken cancellationToken, string actor)
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

    private async Task<string?> ReadTargetStatusAsync(string agent, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM mail_wake_targets WHERE agent = @agent";
        command.Parameters.AddWithValue("@agent", agent);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the actor's active batch id and attempt id.
    /// </summary>
    private async Task<(string BatchId, string AttemptId)> ReadActiveBatchAsync(
        string actor, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT batch_id, attempt_id FROM mail_wake_batches WHERE actor = @actor AND status = 'active'";
        command.Parameters.AddWithValue("@actor", actor);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (reader.GetString(0), reader.GetString(1));
    }

    private async Task<string?> ReadLeaderOwnerTokenAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT owner_token FROM mail_wake_daemons WHERE id = 1";
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> ReadLeaderExpiresAtAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT expires_at FROM mail_wake_daemons WHERE id = 1";
        var value = (string?)await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? null : DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task InsertDueOutboxRowAsync(CancellationToken cancellationToken, string actor = Actor)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES (@actor, 1, 0, @now, @now)
            """;
        command.Parameters.AddWithValue("@actor", actor);
        command.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.AddSeconds(-1));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? s_waitTimeout);

        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + s_waitTimeout;

        while (!await condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken);
        }
    }
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except the first
/// <see cref="TryAcquireAsync"/> call, which throws a non-busy
/// <see cref="SqliteException"/> instead.
/// </summary>
internal sealed class FaultingLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private int _acquireCalls;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _acquireCalls) == 1)
        {
            throw new SqliteException("readonly", 8); // SQLITE_READONLY, not SQLITE_BUSY/LOCKED.
        }

        return inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);
    }

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that the first
/// <paramref name="busyAcquireCalls"/> calls to <see cref="TryAcquireAsync"/>
/// throw a <see cref="SqliteException"/> for SQLITE_BUSY instead.
/// </summary>
internal sealed class BusyLeaderStore(IMailWakeDaemonLeaderStore inner, int busyAcquireCalls)
    : IMailWakeDaemonLeaderStore
{
    private int _acquireCalls;

    public int AcquireCalls => Volatile.Read(ref _acquireCalls);

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _acquireCalls) <= busyAcquireCalls)
        {
            throw new SqliteException("busy", 5); // SQLITE_BUSY
        }

        return inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);
    }

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that the first
/// <paramref name="busyReleaseCalls"/> calls to <see cref="TryReleaseAsync"/>
/// throw a <see cref="SqliteException"/> for SQLITE_BUSY instead. Each
/// attempt (busy or not) is optionally logged to <paramref name="events"/>
/// as <c>"release-attempted"</c>, for asserting ordering against other
/// recorded events.
/// </summary>
internal sealed class BusyReleaseLeaderStore(
    IMailWakeDaemonLeaderStore inner, int busyReleaseCalls, ConcurrentQueue<string>? events = null)
    : IMailWakeDaemonLeaderStore
{
    private int _releaseCalls;

    public int ReleaseCalls => Volatile.Read(ref _releaseCalls);

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        events?.Enqueue("release-attempted");

        if (Interlocked.Increment(ref _releaseCalls) <= busyReleaseCalls)
        {
            throw new SqliteException("busy", 5); // SQLITE_BUSY
        }

        return inner.TryReleaseAsync(token, now, cancellationToken);
    }
}

/// <summary>
/// A per-actor <see cref="IActorWakeDispatcher"/> fake: <paramref name="deniedActor"/>'s
/// first dispatch waits until every other actor's dispatch has registered
/// its own cancellation callback, then returns a pending, access-denied
/// receipt; every other actor hangs until its <see cref="CancellationToken"/>
/// fires, recording <c>"{actor}-cancelled"</c> into <paramref name="events"/>
/// from that callback, and returns only after the callback has run.
/// </summary>
internal sealed class DeniedThenHangingDispatcher(string deniedActor, ConcurrentQueue<string> events)
    : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _hungRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _deniedDispatchCount;

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        if (actor == deniedActor && Interlocked.Increment(ref _deniedDispatchCount) == 1)
        {
            // Never race ahead of the sibling actually being in flight.
            await _hungRegistered.Task;

            return new ActorWakeReceipt(
                actor,
                "denied",
                [new ActorWakeTargetReceipt(actor, MailWakeTargetStatus.Pending, null, null, "access-denied")]);
        }

        var cancelled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = cancellationToken.Register(() =>
        {
            events.Enqueue($"{actor}-cancelled");
            cancelled.TrySetResult();
        });
        _hungRegistered.TrySetResult();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }

        // Keep the registration alive until its callback has recorded the event.
        await cancelled.Task;

        return null;
    }
}

/// <summary>
/// Records the peak number of concurrent <see cref="DispatchAsync"/> calls
/// and every actor a call completed for, without touching any real session,
/// mail, or dispatch machinery.
/// </summary>
internal sealed class ConcurrencyTrackingDispatcher : IActorWakeDispatcher
{
    private int _concurrent;
    private int _maxObservedConcurrency;

    public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);

    public ConcurrentBag<string> CompletedActors { get; } = [];

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var observed = Interlocked.Increment(ref _concurrent);
        InterlockedMax(ref _maxObservedConcurrency, observed);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _concurrent);
        }

        CompletedActors.Add(actor);
        return null;
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
/// Blocks every <see cref="DispatchAsync"/> call on an internal gate until
/// <see cref="Release"/> is called, ignoring the call's own cancellation
/// token.
/// </summary>
internal sealed class HangingDispatcher : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _enteredCount;

    public int EnteredCount => Volatile.Read(ref _enteredCount);

    public void Release() => _gate.TrySetResult();

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _enteredCount);
        await _gate.Task;
        return null;
    }
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that once
/// <see cref="FailNextRenewal"/> has been called, every subsequent
/// <see cref="TryRenewAsync"/> call returns false without reaching
/// <paramref name="inner"/>.
/// </summary>
internal sealed class RenewalLossLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private volatile bool _failRenewal;

    public void FailNextRenewal() => _failRenewal = true;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => _failRenewal
            ? Task.FromResult(false)
            : inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}

/// <summary>
/// Counts every <see cref="DispatchAsync"/> call without touching any real
/// session, mail, or dispatch machinery.
/// </summary>
internal sealed class CountingDispatcher : IActorWakeDispatcher
{
    private int _dispatchCount;

    public int DispatchCount => Volatile.Read(ref _dispatchCount);

    public Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _dispatchCount);
        return Task.FromResult<ActorWakeReceipt?>(null);
    }
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that after
/// <see cref="HoldNextRenewal"/> the next <see cref="TryRenewAsync"/> call
/// awaits <see cref="ReleaseHeldRenewal"/> before delegating.
/// <see cref="RenewalCompleted"/> resolves with that delegated call's result.
/// </summary>
internal sealed class GatedRenewalLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private readonly TaskCompletionSource<bool> _renewalCompleted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private TaskCompletionSource? _gate;

    public Task<bool> RenewalCompleted => _renewalCompleted.Task;

    public void HoldNextRenewal() => _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void ReleaseHeldRenewal() => _gate?.TrySetResult();

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public async Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (_gate is { } gate)
        {
            await gate.Task;
        }

        var renewed = await inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);
        _renewalCompleted.TrySetResult(renewed);
        return renewed;
    }

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}
