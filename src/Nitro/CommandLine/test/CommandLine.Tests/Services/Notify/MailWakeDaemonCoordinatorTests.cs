using System.Collections.Concurrent;
using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeDaemonCoordinator"/> against a real workspace
/// database with a fast injected <see cref="MailWakeDaemonPolicy"/> and real
/// wall-clock time (no <c>FakeTimeProvider</c>, so there is no race between
/// advancing a fake clock and a background loop reaching its next await):
/// first acquisition, staying standby behind another owner's live lease,
/// taking over once that lease expires, exactly one leader winning when two
/// coordinators race the same instance, the admission/execution loops
/// actually draining outstanding actor wake work through the reused
/// <see cref="IActorWakeDispatcher"/>, self-degradation on a daemon-side
/// Claude access denial without disturbing the winning standby, and a
/// bounded, leadership-releasing graceful stop.
/// </summary>
public sealed class MailWakeDaemonCoordinatorTests : IDisposable
{
    private const string Actor = "codex-worker";
    private const string InstanceId = "host-1";

    private static readonly MailWakeDaemonPolicy FastPolicy = new(
        LeaderLeaseDuration: TimeSpan.FromMilliseconds(400),
        HeartbeatInterval: TimeSpan.FromMilliseconds(60),
        AdmissionPollInterval: TimeSpan.FromMilliseconds(30),
        StandbyPollInterval: TimeSpan.FromMilliseconds(30),
        MaxConcurrentActorExecutions: 4,
        ShutdownWait: TimeSpan.FromSeconds(2));

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentSessionRegistry _sessions;
    private readonly MailStore _mail;
    private readonly MailWakeBatchStore _batches;
    private readonly SessionGateCoordinator _gateCoordinator;
    private readonly FixedInstanceIdProvider _instanceIdProvider = new(InstanceId);
    private readonly FixedGlobalConfigDirectoryProvider _globalConfigDirectoryProvider;

    public MailWakeDaemonCoordinatorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-daemon-coordinator-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _agentRegistry = new AgentRegistry(_fileSystem, TimeProvider.System, _database);
        _sessions = new AgentSessionRegistry(
            _fileSystem,
            TimeProvider.System,
            _database,
            _agentRegistry,
            _instanceIdProvider,
            new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName),
            new ProcessInfoProvider(),
            new FixedAncestorSessionResolver(null));
        _globalConfigDirectoryProvider = new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName);
        _mail = new MailStore(
            _fileSystem, TimeProvider.System, _database, _agentRegistry, _instanceIdProvider,
            _globalConfigDirectoryProvider);
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        var gates = new SessionPingGateStore(_fileSystem, _database);
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
        Assert.Equal(1, coordinator.Status.Epoch);
        Assert.Equal(await ReadLeaderOwnerIdAsync(cancellationToken), coordinator.Status.OwnerId);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_StayStandby_When_AnotherOwnerAlreadyHoldsALiveLease()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        await leaderStore.TryAcquireAsync(
            InstanceId, "other-owner", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30), cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinator.StartAsync(cancellationToken);
        await Task.Delay(FastPolicy.StandbyPollInterval * 5, cancellationToken);

        // assert: never became ready while the other owner's lease is live.
        Assert.Equal(MailWakeDaemonState.Standby, coordinator.Status.State);
        Assert.Null(coordinator.Status.Epoch);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StandbyCoordinator_Should_AcquireLeadership_When_ThePriorLeaseExpires()
    {
        // arrange: a short-lived lease held by a different owner, acquired
        // directly through the store (standing in for a leader that crashed
        // without releasing).
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var firstEpoch = await leaderStore.TryAcquireAsync(
            InstanceId, "other-owner", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(100), cancellationToken);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert: took over with a fresh, incremented epoch.
        Assert.Equal(1, firstEpoch);
        Assert.Equal(2, coordinator.Status.Epoch);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task TwoCoordinators_Should_ElectExactlyOneLeader_When_TheyRaceTheSameInstance()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await using var coordinatorA = CreateCoordinator(new FakePingSessionExecutor());
        await using var coordinatorB = CreateCoordinator(new FakePingSessionExecutor());

        // act
        await coordinatorA.StartAsync(cancellationToken);
        await coordinatorB.StartAsync(cancellationToken);
        await WaitUntilAsync(
            () => coordinatorA.Status.State == MailWakeDaemonState.Ready
                || coordinatorB.Status.State == MailWakeDaemonState.Ready,
            cancellationToken);
        await Task.Delay(FastPolicy.StandbyPollInterval * 5, cancellationToken);

        // assert: exactly one became ready with epoch 1, the other stayed
        // standby.
        var states = new[] { coordinatorA.Status.State, coordinatorB.Status.State };
        Assert.Single(states, s => s == MailWakeDaemonState.Ready);
        Assert.Single(states, s => s == MailWakeDaemonState.Standby);
        var readyEpoch = coordinatorA.Status.State == MailWakeDaemonState.Ready
            ? coordinatorA.Status.Epoch
            : coordinatorB.Status.Epoch;
        Assert.Equal(1, readyEpoch);

        await coordinatorA.StopAsync(cancellationToken);
        await coordinatorB.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DispatchOutstandingActorWork_Through_TheAdmissionAndExecutionLoops()
    {
        // arrange: mail enqueued for an actor with a live claimed session,
        // but no direct-first dispatch was ever attempted for it - only the
        // coordinator's own admission/execution loops can find and drain it.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => !executor.Calls.IsEmpty, cancellationToken);
        await WaitUntilAsync(
            async () => await ReadTargetStatusAsync(generation.SessionId, cancellationToken) == MailWakeTargetStatus.Delivered,
            cancellationToken);

        // assert
        Assert.Single(executor.Calls);
        Assert.Equal(MailWakeDaemonState.Ready, coordinator.Status.State);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DegradeAndReleaseLeadership_When_ItsOwnDispatchIsAccessDenied()
    {
        // arrange: the daemon's own attempt at the only live target is
        // itself denied Claude socket access.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor();
        executor.ReasonBySessionId[generation.SessionId] = PingAttemptReason.AccessDenied;
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Degraded, cancellationToken);

        // assert: degraded with the denial recorded, and it does not flap
        // back to ready on its own within a few more poll cycles.
        Assert.Equal("access-denied", coordinator.Status.LastError);
        await Task.Delay(FastPolicy.StandbyPollInterval * 5, cancellationToken);
        Assert.NotEqual(MailWakeDaemonState.Ready, coordinator.Status.State);

        // a differently privileged standby (a second coordinator instance)
        // can take over immediately, without waiting out the lease.
        await using var standby = CreateCoordinator(new FakePingSessionExecutor());
        await standby.StartAsync(cancellationToken);
        await WaitUntilAsync(() => standby.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        Assert.Equal(2, standby.Status.Epoch);

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

        // assert: returned well within the shutdown budget, and a fresh
        // acquire attempt succeeds immediately rather than waiting out the
        // lease duration.
        Assert.True(stopwatch.Elapsed < FastPolicy.ShutdownWait, $"StopAsync took {stopwatch.Elapsed}.");
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var reacquired = await leaderStore.TryAcquireAsync(
            InstanceId, "someone-else", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.NotNull(reacquired);
    }

    [Fact]
    public async Task StartAsync_Should_RecoverAndBecomeReady_When_TheLeaderStoreFaultsOnce()
    {
        // arrange: the leader store itself throws a non-busy exception on
        // the very first acquire attempt, standing in for an unexpected
        // infrastructure fault on the election path.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var faultingStore = new FaultingLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), faultingStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.LastError == "readonly", cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert: the fault was recorded rather than killing the loop, and
        // the next election attempt still reached Ready. StopAsync must not
        // rethrow the earlier fault either.
        Assert.Equal(MailWakeDaemonState.Ready, coordinator.Status.State);
        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DemoteToStandby_And_CancelInFlightDispatch_When_RenewalIsLost()
    {
        // arrange: a live session with an in-flight, never-returning
        // transport call, then the leader's next heartbeat renewal is made
        // to fail as if a fresher claimant had taken the lease.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var generation = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken);
        var executor = new FakePingSessionExecutor { HangUntilCancelled = true };
        var renewalLossStore = new RenewalLossLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        await using var coordinator = CreateCoordinator(executor, renewalLossStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await executor.Entered.Task.WaitAsync(WaitTimeout, cancellationToken);
        renewalLossStore.FailNextRenewal();
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Standby, cancellationToken);

        // assert: demoted, and the hung dispatch never got to record a
        // delivery for the target it was cancelled mid-flight on.
        var status = await ReadTargetStatusAsync(generation.SessionId, cancellationToken);
        Assert.NotEqual(MailWakeTargetStatus.Delivered, status);
        Assert.Null(coordinator.Status.OwnerId);
        Assert.Null(coordinator.Status.Epoch);
        Assert.Null(coordinator.Status.LeaseExpiresAt);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_AdmitASecondActor_While_TheFirstActorsTransportIsBlocked()
    {
        // arrange: two actors, each with a live session and enqueued mail;
        // every transport call hangs until cancelled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        const string secondActor = "codex-worker-2";
        await SeedLiveSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken, sessionId: "session-1", actor: Actor);
        await SeedLiveSessionAsync(
            AgentSessionEndpointKind.CodexThread, "thread-2", cancellationToken, sessionId: "session-2",
            actor: secondActor);
        await SendEnqueuedMailAsync(cancellationToken, Actor);
        await SendEnqueuedMailAsync(cancellationToken, secondActor);
        var executor = new FakePingSessionExecutor { HangUntilCancelled = true };
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(500);

        while (executor.Calls.Select(c => c.SessionId).Distinct().Count() < 2)
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("The second actor was not admitted within 500 ms.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
        }

        // assert: both actors were admitted concurrently, well within
        // 500 ms of the first call, while neither transport had completed.
        Assert.Equal(2, executor.Calls.Select(c => c.SessionId).Distinct().Count());

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_IgnoreOutboxRows_Of_AnotherNitroInstance()
    {
        // arrange: a due outbox row exists, but for a different Nitro
        // instance; this instance itself has no outstanding work at all.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await InsertDueOutboxRowAsync("host-2", cancellationToken);
        var executor = new FakePingSessionExecutor();
        await using var coordinator = CreateCoordinator(executor);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        await Task.Delay(FastPolicy.AdmissionPollInterval * 5, cancellationToken);

        // assert: never dispatched to, or touched, the other instance's row.
        Assert.Empty(executor.Calls);
        var row = await ReadOutboxRowAsync("host-2", cancellationToken);
        Assert.Equal((0L, 1L), row);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_BecomeReady_When_TheLeaderStoreIsBusyTwice()
    {
        // arrange: the leader store throws SQLITE_BUSY on the first two
        // acquire attempts, then succeeds.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var busyStore = new BusyLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), busyAcquireCalls: 2);
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), busyStore);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert: retried through both busy attempts and became ready on the
        // third, without recording an error.
        Assert.Equal(3, busyStore.AcquireCalls);
        Assert.Null(coordinator.Status.LastError);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_GiveUpTheTick_And_RetryOnTheNextStandbyPoll_When_BusyRetriesAreExhausted()
    {
        // arrange: the leader store throws SQLITE_BUSY on the first five
        // acquire attempts, exhausting one tick's busy retries, then
        // succeeds on the next standby poll's own first attempt.
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

        // assert: five attempts exhausted the first tick's busy retries, and
        // a sixth attempt on the next standby poll succeeded.
        Assert.Equal(6, busyStore.AcquireCalls);
        Assert.Null(coordinator.Status.LastError);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_APriorStopTimedOut_And_TheRunLoopIsStillAlive()
    {
        // arrange: the only outstanding actor's dispatch hangs forever and
        // ignores cancellation, so StopAsync's shutdown budget is exceeded
        // and the run loop is still alive when it returns.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await InsertDueOutboxRowAsync(InstanceId, cancellationToken);
        var shortShutdownPolicy = FastPolicy with { ShutdownWait = TimeSpan.FromMilliseconds(50) };
        var dispatcher = new HangingDispatcher();
        var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            dispatcher,
            _fileSystem,
            _database,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            TimeProvider.System,
            shortShutdownPolicy);
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);
        await WaitUntilAsync(() => dispatcher.EnteredCount > 0, cancellationToken);
        await coordinator.StopAsync(cancellationToken);

        // act & assert: a still-alive orphaned run loop keeps StartAsync's
        // guard throwing for this instance.
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartAsync(cancellationToken));

        // release the hung dispatch and wait for the orphaned run loop to
        // actually finish releasing leadership before disposing: the timed
        // out StopAsync above already disposed the run loop's linked
        // CancellationTokenSource, so this DisposeAsync's own CancelAsync
        // call throws ObjectDisposedException before it ever awaits the run
        // task, and its exception handler only snapshots IsCompleted once
        // rather than waiting for it. Waiting on the leader row here instead
        // means the run loop's SQLite connection is closed before the
        // fixture's Dispose deletes the temp workspace.
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
        // arrange: one actor's dispatch is denied Claude socket access, a
        // second actor's dispatch hangs on transport, and the leader
        // store's release throws SQLITE_BUSY once before succeeding.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        const string deniedActor = "denied-actor";
        const string hungActor = "hung-actor";
        await _agentRegistry.EnsureImplicitAsync(deniedActor, cancellationToken);
        await _agentRegistry.EnsureImplicitAsync(hungActor, cancellationToken);
        await InsertDueOutboxRowAsync(InstanceId, cancellationToken, deniedActor);
        await InsertDueOutboxRowAsync(InstanceId, cancellationToken, hungActor);
        var events = new ConcurrentQueue<string>();
        var dispatcher = new DeniedThenHangingDispatcher(deniedActor, events);
        var busyReleaseStore = new BusyReleaseLeaderStore(
            new MailWakeDaemonLeaderStore(_fileSystem, _database), busyReleaseCalls: 1, events);
        await using var coordinator = new MailWakeDaemonCoordinator(
            busyReleaseStore,
            dispatcher,
            _fileSystem,
            _database,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            TimeProvider.System,
            FastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Degraded, cancellationToken);

        // the busy-retried release only completes ~250ms (the policy's
        // initial backoff) after the first, busy attempt, so wait for the
        // actual retry rather than for Degraded alone.
        await WaitUntilAsync(() => busyReleaseStore.ReleaseCalls >= 2, cancellationToken);

        // assert: the release retried through the busy attempt and
        // succeeded, and the hung actor's dispatch observed cancellation
        // strictly before the release was even attempted.
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
        Assert.Equal(2, standby.Status.Epoch);

        await coordinator.StopAsync(cancellationToken);
        await standby.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_NeverExceedMaxConcurrentActorExecutions_When_MoreActorsAreDueThanTheLimit()
    {
        // arrange: five due actors under a policy capped at four concurrent
        // executions; every dispatch call takes long enough that the fifth
        // actor can only be admitted once one of the first four completes.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var actors = Enumerable.Range(1, 5).Select(i => $"actor-{i}").ToArray();

        foreach (var actor in actors)
        {
            await _agentRegistry.EnsureImplicitAsync(actor, cancellationToken);
            await InsertDueOutboxRowAsync(InstanceId, cancellationToken, actor);
        }

        var dispatcher = new ConcurrencyTrackingDispatcher();
        await using var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            dispatcher,
            _fileSystem,
            _database,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            TimeProvider.System,
            FastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await WaitUntilAsync(() => dispatcher.CompletedActors.Distinct().Count() >= actors.Length, cancellationToken);

        // assert: the gate's own capacity was actually reached, and never
        // exceeded, while draining all five actors.
        Assert.Equal(FastPolicy.MaxConcurrentActorExecutions, dispatcher.MaxObservedConcurrency);

        await coordinator.StopAsync(cancellationToken);
    }

    private MailWakeDaemonCoordinator CreateCoordinator(
        FakePingSessionExecutor executor, IMailWakeDaemonLeaderStore? leaderStore = null)
        => new(
            leaderStore ?? new MailWakeDaemonLeaderStore(_fileSystem, _database),
            new ActorWakeDispatcher(
                _batches,
                _sessions,
                _gateCoordinator,
                executor,
                _mail,
                _instanceIdProvider,
                _globalConfigDirectoryProvider,
                TimeProvider.System),
            _fileSystem,
            _database,
            _instanceIdProvider,
            _globalConfigDirectoryProvider,
            TimeProvider.System,
            FastPolicy);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<AgentSessionGeneration> SeedLiveSessionAsync(
        string endpointKind, string endpointAddr, CancellationToken cancellationToken,
        string sessionId = "session-1", string actor = Actor)
    {
        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;
        var generation = new AgentSessionGeneration(harness, sessionId, InstanceId, pid, procStart);

        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", endpointKind, endpointAddr, envActor: actor,
            cancellationToken);

        return generation;
    }

    private async Task<MailMessage> SendEnqueuedMailAsync(CancellationToken cancellationToken, string actor = Actor)
        => await _mail.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = "pascal",
                Subject = "status",
                Body = "check",
                To = [actor],
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);

    private async Task<string?> ReadTargetStatusAsync(string sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM mail_wake_targets WHERE session_id = @sessionId";
        command.Parameters.AddWithValue("@sessionId", sessionId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<string?> ReadLeaderOwnerIdAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT owner_id FROM mail_wake_daemons WHERE nitro_instance_id = @nitroInstanceId";
        command.Parameters.AddWithValue("@nitroInstanceId", InstanceId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> ReadLeaderExpiresAtAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT expires_at FROM mail_wake_daemons WHERE nitro_instance_id = @nitroInstanceId";
        command.Parameters.AddWithValue("@nitroInstanceId", InstanceId);
        var value = (string?)await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    private async Task InsertDueOutboxRowAsync(
        string nitroInstanceId, CancellationToken cancellationToken, string actor = Actor)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_outbox (
                nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at
            )
            VALUES (@nitroInstanceId, @actor, 1, 0, @now, @now)
            """;
        command.Parameters.AddWithValue("@nitroInstanceId", nitroInstanceId);
        command.Parameters.AddWithValue("@actor", actor);
        command.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.AddSeconds(-1));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<(long SettledGeneration, long RequestedGeneration)> ReadOutboxRowAsync(
        string nitroInstanceId, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT settled_generation, requested_generation FROM mail_wake_outbox "
            + "WHERE nitro_instance_id = @nitroInstanceId";
        command.Parameters.AddWithValue("@nitroInstanceId", nitroInstanceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? WaitTimeout);

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
        var deadline = DateTime.UtcNow + WaitTimeout;

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

    public Task<long?> TryAcquireAsync(
        string nitroInstanceId, string ownerId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _acquireCalls) == 1)
        {
            throw new SqliteException("readonly", 8); // SQLITE_READONLY, not SQLITE_BUSY/LOCKED.
        }

        return inner.TryAcquireAsync(nitroInstanceId, ownerId, now, leaseDuration, cancellationToken);
    }

    public Task<bool> TryRenewAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, TimeSpan leaseDuration,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryRenewAsync(nitroInstanceId, ownerId, epoch, now, leaseDuration, lastError, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(nitroInstanceId, ownerId, epoch, now, cancellationToken);
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

    public Task<long?> TryAcquireAsync(
        string nitroInstanceId, string ownerId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _acquireCalls) <= busyAcquireCalls)
        {
            throw new SqliteException("busy", 5); // SQLITE_BUSY
        }

        return inner.TryAcquireAsync(nitroInstanceId, ownerId, now, leaseDuration, cancellationToken);
    }

    public Task<bool> TryRenewAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, TimeSpan leaseDuration,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryRenewAsync(nitroInstanceId, ownerId, epoch, now, leaseDuration, lastError, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(nitroInstanceId, ownerId, epoch, now, cancellationToken);
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

    public Task<long?> TryAcquireAsync(
        string nitroInstanceId, string ownerId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryAcquireAsync(nitroInstanceId, ownerId, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, TimeSpan leaseDuration,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryRenewAsync(nitroInstanceId, ownerId, epoch, now, leaseDuration, lastError, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, CancellationToken cancellationToken)
    {
        events?.Enqueue("release-attempted");

        if (Interlocked.Increment(ref _releaseCalls) <= busyReleaseCalls)
        {
            throw new SqliteException("busy", 5); // SQLITE_BUSY
        }

        return inner.TryReleaseAsync(nitroInstanceId, ownerId, epoch, now, cancellationToken);
    }
}

/// <summary>
/// A per-actor <see cref="IActorWakeDispatcher"/> fake: <paramref name="deniedActor"/>'s
/// first dispatch waits until every other actor's dispatch has registered
/// its own cancellation callback, then returns a pending, access-denied
/// receipt; every other actor hangs until its <see cref="CancellationToken"/>
/// fires, recording <c>"{actor}-cancelled"</c> into <paramref name="events"/>
/// from that callback, before observing the cancellation itself.
/// </summary>
internal sealed class DeniedThenHangingDispatcher(string deniedActor, ConcurrentQueue<string> events)
    : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _hungRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _deniedDispatchCount;

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        if (actor == deniedActor && Interlocked.Increment(ref _deniedDispatchCount) == 1)
        {
            // Never race ahead of the sibling actually being in flight.
            await _hungRegistered.Task;

            var target = new AgentSessionGeneration(
                AgentSessionHarness.ClaudeCode, "denied-session", "host-1", Environment.ProcessId, "0");

            return new ActorWakeReceipt(
                actor,
                "denied",
                [new ActorWakeTargetReceipt(target, MailWakeTargetStatus.Pending, null, null, "access-denied")]);
        }

        await using var registration = cancellationToken.Register(() => events.Enqueue($"{actor}-cancelled"));
        _hungRegistered.TrySetResult();

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }

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
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
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
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
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

    public Task<long?> TryAcquireAsync(
        string nitroInstanceId, string ownerId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryAcquireAsync(nitroInstanceId, ownerId, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, TimeSpan leaseDuration,
        string? lastError, CancellationToken cancellationToken)
        => _failRenewal
            ? Task.FromResult(false)
            : inner.TryRenewAsync(nitroInstanceId, ownerId, epoch, now, leaseDuration, lastError, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string nitroInstanceId, string ownerId, long epoch, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(nitroInstanceId, ownerId, epoch, now, cancellationToken);
}
