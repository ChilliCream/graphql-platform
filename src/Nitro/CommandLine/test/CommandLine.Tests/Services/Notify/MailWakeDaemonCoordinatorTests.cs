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
/// Every test drives time exclusively through a <see cref="FakeTimeProvider"/> and
/// synchronizes with the coordinator's background loops through its test hooks and
/// <see cref="TaskCompletionSource"/> signals; the only real-time waits are generous
/// hang guards on those signals, never something a test relies on to pass.
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

    private static readonly TimeSpan s_hangGuard = TimeSpan.FromSeconds(60);

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly MailWakeBatchStore _batches;
    private readonly SessionGateCoordinator _gateCoordinator;
    private AgentRegistry _agentRegistry = null!;
    private AgentStore _agentStore = null!;
    private MailStore _mail = null!;

    public MailWakeDaemonCoordinatorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-daemon-coordinator-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _batches = new MailWakeBatchStore(_fileSystem, _database);
        var gates = new AgentPingGateStore(_fileSystem, _database);
        var leases = new PingLeaseStore(_fileSystem, _database);
        _gateCoordinator = new SessionGateCoordinator(gates, leases);
    }

    /// <summary>
    /// Builds the agent registry, agent store and mail store from <paramref name="timeProvider"/>,
    /// so every store the coordinator reads or writes through shares the test's fake clock.
    /// </summary>
    private void CreateStores(FakeTimeProvider timeProvider)
    {
        _agentRegistry = new AgentRegistry(_fileSystem, timeProvider, _database);
        _agentStore = new AgentStore(_fileSystem, timeProvider, _database);
        _mail = new MailStore(_fileSystem, timeProvider, _database, _agentStore);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task StartAsync_Should_BecomeReady_When_NoOtherLeaderExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), timeProvider, ticks: ticks);

        // act
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        await leaderStore.TryAcquireAsync(
            "other-owner", timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), timeProvider, ticks: ticks);

        // act
        await coordinator.StartAsync(cancellationToken);

        for (var i = 0; i < 5; i++)
        {
            var tick = ticks.WaitForNextTickAsync(cancellationToken);
            timeProvider.Advance(s_fastPolicy.StandbyPollInterval);
            await tick;
        }

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var firstAcquired = await leaderStore.TryAcquireAsync(
            "other-owner", timeProvider.GetUtcNow(), s_fastPolicy.StandbyPollInterval, cancellationToken);
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), timeProvider, ticks: ticks);

        // act
        await coordinator.StartAsync(cancellationToken);
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.StandbyPollInterval,
            ticks.WaitForNextTickAsync,
            () => coordinator.Status.State == MailWakeDaemonState.Ready,
            cancellationToken);

        // assert
        // took over with its own token once the prior lease expired.
        Assert.True(firstAcquired);
        Assert.NotNull(coordinator.Status.OwnerToken);
        Assert.NotEqual("other-owner", coordinator.Status.OwnerToken);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_DispatchOutstandingActorWork_When_MailIsEnqueuedForALiveAgent()
    {
        // arrange
        // Enqueue mail for a live agent without calling the dispatcher directly.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor();
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(executor, timeProvider, ticks: ticks);

        // act
        await coordinator.StartAsync(cancellationToken);
        await executor.Entered.Task.WaitAsync(s_hangGuard, cancellationToken);
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.AdmissionPollInterval,
            ticks.WaitForNextTickAsync,
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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.ClaudePeer, "peer-a", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor();
        executor.ReasonByActor[actor] = PingAttemptReason.AccessDenied;
        var releaseStore = new ReleaseSignalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var leadershipEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var standbyTicks = new LoopTickSignal();
        var standbyTickCount = 0;
        await using var coordinator = new MailWakeDaemonCoordinator(
            releaseStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, executor, _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            // Fires once leadership ends, before admission ticks stop.
            AfterLeadershipEndedAsync = _ =>
            {
                leadershipEnded.TrySetResult();
                return Task.CompletedTask;
            },
            AfterStandbyTickAsync = ct =>
            {
                Interlocked.Increment(ref standbyTickCount);
                return standbyTicks.HookAsync(ct);
            }
        };

        // act
        await coordinator.StartAsync(cancellationToken);
        await leadershipEnded.Task.WaitAsync(s_hangGuard, cancellationToken);
        await releaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);

        // The standby loop's first tick needs no clock advance, so wait for it before advancing.
        var maybeFirstStandbyTick = standbyTicks.WaitForNextTickAsync(cancellationToken);

        if (Volatile.Read(ref standbyTickCount) == 0)
        {
            await maybeFirstStandbyTick;
        }

        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.StandbyPollInterval,
            standbyTicks.WaitForNextTickAsync,
            () => Volatile.Read(ref standbyTickCount) >= 5,
            cancellationToken);

        // A differently privileged standby (a second coordinator instance) takes over.
        var standbyReadyTicks = new LoopTickSignal();
        await using var standby = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, new FakePingSessionExecutor(), _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            AfterAdmissionTickAsync = standbyReadyTicks.HookAsync
        };
        await standby.StartAsync(cancellationToken);
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.StandbyPollInterval,
            standbyReadyTicks.WaitForNextTickAsync,
            () => standby.Status.State == MailWakeDaemonState.Ready,
            cancellationToken);

        // assert
        // Degraded with the denial recorded, released without waiting out the lease.
        Assert.Equal(MailWakeDaemonState.Degraded, coordinator.Status.State);
        Assert.Equal("access-denied", coordinator.Status.LastError);
        Assert.NotEqual(MailWakeDaemonState.Ready, coordinator.Status.State);
        Assert.NotNull(standby.Status.OwnerToken);
        Assert.True(timeProvider.GetUtcNow() - timeProvider.Start < s_fastPolicy.LeaderLeaseDuration);

        await coordinator.StopAsync(cancellationToken);
        await standby.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StopAsync_Should_ReturnWithinTheShutdownBudget_And_ReleaseLeadershipImmediately_When_CalledWhileLeading()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), timeProvider, ticks: ticks);
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;

        // act
        await coordinator.StopAsync(cancellationToken);

        // assert
        // Returned without the fake clock ever moving, and the lease is reacquirable immediately.
        Assert.Equal(timeProvider.Start, timeProvider.GetUtcNow());
        var leaderStore = new MailWakeDaemonLeaderStore(_fileSystem, _database);
        var reacquired = await leaderStore.TryAcquireAsync(
            "someone-else", timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);
        Assert.True(reacquired);
    }

    [Fact]
    public async Task StartAsync_Should_RecoverAndBecomeReady_When_TheLeaderStoreFaultsOnce()
    {
        // arrange
        // The leader store throws a non-busy exception on the first acquire attempt.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var faultingStore = new FaultingLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var ticks = new LoopTickSignal();
        var retryDelayArmed = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(
            new FakePingSessionExecutor(), timeProvider, faultingStore, ticks, retryDelayArmed);

        // act
        var armed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await faultingStore.Faulted.WaitAsync(s_hangGuard, cancellationToken);
        await armed;
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        timeProvider.Advance(s_fastPolicy.StandbyPollInterval);
        await ready;

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
        var executor = new FakePingSessionExecutor { HangUntilCancelled = true };
        // A lost renewal also releases the lease, so nothing else contests it before the asserts run.
        var releaseStore = new ReleaseSignalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var renewalLossStore = new RenewalLossLeaderStore(releaseStore);
        var leadershipEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var coordinator = new MailWakeDaemonCoordinator(
            renewalLossStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, executor, _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            // Fires only after the coordinator's own status update, safe to assert against.
            AfterLeadershipEndedAsync = _ =>
            {
                leadershipEnded.TrySetResult();
                return Task.CompletedTask;
            }
        };

        // act
        await coordinator.StartAsync(cancellationToken);
        await executor.Entered.Task.WaitAsync(s_hangGuard, cancellationToken);
        renewalLossStore.FailNextRenewal();
        timeProvider.Advance(s_fastPolicy.HeartbeatInterval);
        await leadershipEnded.Task.WaitAsync(s_hangGuard, cancellationToken);
        await releaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var actor = await SeedLiveSessionAsync(AgentSessionEndpointKind.CodexThread, "thread-1", cancellationToken);
        await SendEnqueuedMailAsync(cancellationToken, actor);
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
        var ticksB = new LoopTickSignal();
        await using var coordinatorB = new MailWakeDaemonCoordinator(
            sharedLeaderStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, new FakePingSessionExecutor(), _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            AfterAdmissionTickAsync = ticksB.HookAsync
        };

        // act
        await coordinatorA.StartAsync(cancellationToken);
        await executorA.Entered.Task.WaitAsync(s_hangGuard, cancellationToken);
        var staleToken = coordinatorA.Status.OwnerToken!;
        gatedLeaderStore.HoldNextRenewal();
        timeProvider.Advance(s_fastPolicy.LeaderLeaseDuration + s_fastPolicy.HeartbeatInterval);
        var aStandbyWhileRenewalHeld = coordinatorA.Status.State;

        var readyB = ticksB.WaitForNextTickAsync(cancellationToken);
        await coordinatorB.StartAsync(cancellationToken);
        await readyB;
        gatedLeaderStore.ReleaseHeldRenewal();
        await gatedLeaderStore.RenewalCompleted.WaitAsync(s_hangGuard, cancellationToken);
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
        CreateStores(timeProvider);
        var gatedLeaderStore = new GatedRenewalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var dispatcher = new CountingDispatcher();
        var ticks = new LoopTickSignal();
        var ticksAfterInsert = 0;
        DateTimeOffset? insertedAt = null;
        await using var coordinator = new MailWakeDaemonCoordinator(
            gatedLeaderStore, dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy)
        {
            AfterAdmissionTickAsync = ct =>
            {
                if (insertedAt is { } insertedAtValue && timeProvider.GetUtcNow() > insertedAtValue)
                {
                    Interlocked.Increment(ref ticksAfterInsert);
                }

                return ticks.HookAsync(ct);
            }
        };

        // act
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;
        gatedLeaderStore.HoldNextRenewal();
        timeProvider.Advance(s_fastPolicy.LeaderLeaseDuration + s_fastPolicy.HeartbeatInterval);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, Actor);
        insertedAt = timeProvider.GetUtcNow();
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.AdmissionPollInterval,
            ticks.WaitForNextTickAsync,
            () => Volatile.Read(ref ticksAfterInsert) > 0,
            cancellationToken);
        gatedLeaderStore.ReleaseHeldRenewal();

        // assert
        // The lapsed cached lease stopped admission before the heartbeat's held renewal ever returned.
        Assert.Equal(0, dispatcher.DispatchCount);
        Assert.Equal(MailWakeDaemonState.Standby, coordinator.Status.State);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_ReleaseLeadershipAfterTheHeldRenewalReturns_When_ItsOwnDispatchIsAccessDenied()
    {
        // arrange
        // The heartbeat's renewal is held in flight while an access-denied dispatch degrades the leader.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        var events = new ConcurrentQueue<string>();
        var leaderStore = new OrderedReleaseLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), events);
        var dispatcher = new AlwaysDeniedDispatcher();
        var ticks = new LoopTickSignal();
        await using var coordinator = new MailWakeDaemonCoordinator(
            leaderStore, dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy)
        {
            AfterAdmissionTickAsync = ticks.HookAsync
        };

        // act
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;
        leaderStore.HoldNextRenewal();
        timeProvider.Advance(s_fastPolicy.HeartbeatInterval);
        await leaderStore.HeldEntered.WaitAsync(s_hangGuard, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, Actor);
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.AdmissionPollInterval,
            ticks.WaitForNextTickAsync,
            () => coordinator.Status.State == MailWakeDaemonState.Degraded,
            cancellationToken);
        leaderStore.ReleaseHeldRenewal();
        await leaderStore.Released.WaitAsync(s_hangGuard, cancellationToken);

        // assert
        Assert.Equal(["renewal-completed", "release-attempted"], events);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_AdmitASecondActor_When_TheFirstActorsTransportIsBlocked()
    {
        // arrange
        // Two due actors, both hanging on their dispatch until cancelled.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        const string firstActor = "actor-1";
        const string secondActor = "actor-2";
        await _agentRegistry.EnsureImplicitAsync(firstActor, cancellationToken);
        await _agentRegistry.EnsureImplicitAsync(secondActor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, firstActor);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, secondActor);
        var dispatcher = new ConcurrentEntryDispatcher(expectedActors: 2);
        await using var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database), dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy);

        // act
        await coordinator.StartAsync(cancellationToken);
        await dispatcher.AllEntered.WaitAsync(s_hangGuard, cancellationToken);

        // assert
        // Both actors were admitted concurrently, while neither transport had completed.
        Assert.Equal(2, dispatcher.EnteredCount);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task StartAsync_Should_BecomeReady_When_TheLeaderStoreIsBusyTwice()
    {
        // arrange
        // The leader store throws SQLITE_BUSY on the first two acquire attempts.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var busyStore = new BusyLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), busyAcquireCalls: 2);
        var ticks = new LoopTickSignal();
        var retryDelayArmed = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(
            new FakePingSessionExecutor(), timeProvider, busyStore, ticks, retryDelayArmed);

        // act
        var firstDelayArmed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
        var firstCall = busyStore.WaitForNextCallAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await firstCall;
        await firstDelayArmed;

        var secondDelayArmed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
        var secondCall = busyStore.WaitForNextCallAsync(cancellationToken);
        timeProvider.Advance(MailWakeDaemonRetryPolicy.ComputeDelay(1));
        await secondCall;
        await secondDelayArmed;

        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        timeProvider.Advance(MailWakeDaemonRetryPolicy.ComputeDelay(2));
        await ready;

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var busyStore = new BusyLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database), busyAcquireCalls: 5);
        var standbyTicks = new LoopTickSignal();
        var admissionTicks = new LoopTickSignal();
        var retryDelayArmed = new LoopTickSignal();
        await using var coordinator = new MailWakeDaemonCoordinator(
            busyStore,
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, new FakePingSessionExecutor(), _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            AfterStandbyTickAsync = standbyTicks.HookAsync,
            AfterAdmissionTickAsync = admissionTicks.HookAsync,
            AfterRetryDelayArmedAsync = retryDelayArmed.HookAsync
        };

        // act
        var firstDelayArmed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
        var firstCall = busyStore.WaitForNextCallAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await firstCall;
        await firstDelayArmed;

        for (var attempt = 1; attempt < 4; attempt++)
        {
            var nextDelayArmed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
            var nextCall = busyStore.WaitForNextCallAsync(cancellationToken);
            timeProvider.Advance(MailWakeDaemonRetryPolicy.ComputeDelay(attempt));
            await nextCall;
            await nextDelayArmed;
        }

        // Call #5 exhausts the attempt budget, so it moves straight to the standby-poll hook.
        var fifthCall = busyStore.WaitForNextCallAsync(cancellationToken);
        var standbyTick = standbyTicks.WaitForNextTickAsync(cancellationToken);
        timeProvider.Advance(MailWakeDaemonRetryPolicy.ComputeDelay(4));
        await fifthCall;
        await standbyTick;

        var sixthCall = busyStore.WaitForNextCallAsync(cancellationToken);
        var ready = admissionTicks.WaitForNextTickAsync(cancellationToken);
        timeProvider.Advance(s_fastPolicy.StandbyPollInterval);
        await sixthCall;
        await ready;

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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, Actor);
        var shortShutdownPolicy = s_fastPolicy with { ShutdownWait = TimeSpan.FromMilliseconds(50) };
        var dispatcher = new HangingDispatcher();
        var releaseStore = new ReleaseSignalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var ticks = new LoopTickSignal();
        var stopWaitArmed = new LoopTickSignal();
        var leadershipEndedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var parkGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new MailWakeDaemonCoordinator(
            releaseStore,
            dispatcher,
            _fileSystem,
            _database,
            timeProvider,
            shortShutdownPolicy)
        {
            AfterAdmissionTickAsync = ticks.HookAsync,
            // Parks the run loop before its own drain bound, so StopAsync's bound times out first.
            AfterLeadershipEndedAsync = _ =>
            {
                leadershipEndedEntered.TrySetResult();
                return parkGate.Task;
            },
            AfterShutdownWaitArmedAsync = stopWaitArmed.HookAsync
        };
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;
        await dispatcher.Entered.Task.WaitAsync(s_hangGuard, cancellationToken);

        // act
        // A still-alive orphaned run loop keeps StartAsync's guard throwing for this instance.
        var stopWaitArmedWait = stopWaitArmed.WaitForNextTickAsync(cancellationToken);
        var stopping = coordinator.StopAsync(cancellationToken);
        await stopWaitArmedWait;
        await leadershipEndedEntered.Task.WaitAsync(s_hangGuard, cancellationToken);
        timeProvider.Advance(shortShutdownPolicy.ShutdownWait);
        await stopping;
        var restart = () => coordinator.StartAsync(cancellationToken);

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(restart);

        // Release the parked run loop and the dispatch, then wait for leadership cleanup.
        parkGate.TrySetResult();
        dispatcher.Release();
        await releaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);
        await coordinator.DisposeAsync();
    }

    [Fact]
    public async Task RunningLeader_Should_ReleaseLeadership_And_CancelSiblings_When_AccessDeniedReleaseIsBusy()
    {
        // arrange
        // One actor is denied access, a second hangs on transport, and release throws SQLITE_BUSY once.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        const string deniedActor = "denied-actor";
        const string hungActor = "hung-actor";
        await _agentRegistry.EnsureImplicitAsync(deniedActor, cancellationToken);
        await _agentRegistry.EnsureImplicitAsync(hungActor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, deniedActor);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, hungActor);
        var events = new ConcurrentQueue<string>();
        var dispatcher = new DeniedThenHangingDispatcher(deniedActor, events);
        var busyReleaseStore = new BusyReleaseLeaderStore(
            new MailWakeDaemonLeaderStore(_fileSystem, _database), busyReleaseCalls: 1, events);
        var retryDelayArmed = new LoopTickSignal();
        await using var coordinator = new MailWakeDaemonCoordinator(
            busyReleaseStore, dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy)
        {
            AfterRetryDelayArmedAsync = retryDelayArmed.HookAsync
        };

        // act
        var firstDelayArmed = retryDelayArmed.WaitForNextTickAsync(cancellationToken);
        var firstReleaseAttempt = busyReleaseStore.WaitForNextReleaseAttemptAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await firstReleaseAttempt;
        await firstDelayArmed;

        var secondReleaseAttempt = busyReleaseStore.WaitForNextReleaseAttemptAsync(cancellationToken);
        timeProvider.Advance(MailWakeDaemonRetryPolicy.ComputeDelay(1));
        await secondReleaseAttempt;
        await busyReleaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);

        // assert
        Assert.Equal(2, busyReleaseStore.ReleaseCalls);
        var ordered = events.ToArray();
        var cancelledIndex = Array.IndexOf(ordered, $"{hungActor}-cancelled");
        var releasedIndex = Array.IndexOf(ordered, "release-attempted");
        Assert.True(
            cancelledIndex >= 0 && releasedIndex >= 0 && cancelledIndex < releasedIndex,
            $"Expected \"{hungActor}-cancelled\" before \"release-attempted\". Events: [{string.Join(", ", ordered)}]");

        // A differently privileged standby takes over, proving the release reached the database.
        var standbyReadyTicks = new LoopTickSignal();
        await using var standby = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, new FakePingSessionExecutor(), _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            AfterAdmissionTickAsync = standbyReadyTicks.HookAsync,
            AfterStandbyTickAsync = standbyReadyTicks.HookAsync
        };
        await standby.StartAsync(cancellationToken);
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.StandbyPollInterval,
            standbyReadyTicks.WaitForNextTickAsync,
            () => standby.Status.State == MailWakeDaemonState.Ready,
            cancellationToken);
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
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var actors = Enumerable.Range(1, 5).Select(i => $"actor-{i}").ToArray();

        foreach (var actor in actors)
        {
            await _agentRegistry.EnsureImplicitAsync(actor, cancellationToken);
            await InsertDueOutboxRowAsync(cancellationToken, timeProvider, actor);
        }

        var dispatcher = new ConcurrencyTrackingDispatcher(capacity: s_fastPolicy.MaxConcurrentActorExecutions);
        var ticks = new LoopTickSignal();
        await using var coordinator = new MailWakeDaemonCoordinator(
            new MailWakeDaemonLeaderStore(_fileSystem, _database), dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy)
        {
            AfterAdmissionTickAsync = ticks.HookAsync
        };

        // act
        await coordinator.StartAsync(cancellationToken);
        await dispatcher.CapacityReached.WaitAsync(s_hangGuard, cancellationToken);
        dispatcher.ReleaseAll();
        await AdvanceUntilAsync(
            timeProvider,
            s_fastPolicy.AdmissionPollInterval,
            ticks.WaitForNextTickAsync,
            () => dispatcher.CompletedActors.Distinct().Count() >= actors.Length,
            cancellationToken);

        // assert
        // The gate's capacity was reached, and never exceeded, while draining all five actors.
        Assert.Equal(s_fastPolicy.MaxConcurrentActorExecutions, dispatcher.MaxObservedConcurrency);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_ReleaseLeadershipAndReturnToStandby_When_ItsHeartbeatRenewalThrows()
    {
        // arrange
        // F3: the next heartbeat renewal throws instead of returning false.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        var releaseStore = new ReleaseSignalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var throwingStore = new ThrowingRenewalLeaderStore(releaseStore);
        var ticks = new LoopTickSignal();
        await using var coordinator = CreateCoordinator(new FakePingSessionExecutor(), timeProvider, throwingStore, ticks);

        // act
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;
        throwingStore.FailNextRenewal();
        timeProvider.Advance(s_fastPolicy.HeartbeatInterval);
        await throwingStore.RenewalFaulted.WaitAsync(s_hangGuard, cancellationToken);
        await releaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);
        var reacquired = await new MailWakeDaemonLeaderStore(_fileSystem, _database).TryAcquireAsync(
            "someone-else", timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // assert
        // Demoted locally, and the lease was actually released rather than left held until natural expiry.
        Assert.Equal(MailWakeDaemonState.Standby, coordinator.Status.State);
        Assert.True(reacquired);

        await coordinator.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task RunningLeader_Should_ReleaseLeadershipAfterTheShutdownWait_When_ADispatchIgnoresCancellation()
    {
        // arrange
        // F4: the only due actor's dispatch hangs forever and ignores cancellation, then the heartbeat renewal is lost.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        CreateStores(timeProvider);
        await _agentRegistry.EnsureImplicitAsync(Actor, cancellationToken);
        await InsertDueOutboxRowAsync(cancellationToken, timeProvider, Actor);
        var dispatcher = new HangingDispatcher();
        var releaseStore = new ReleaseSignalLeaderStore(new MailWakeDaemonLeaderStore(_fileSystem, _database));
        var renewalLossStore = new RenewalLossLeaderStore(releaseStore);
        var ticks = new LoopTickSignal();
        var leadershipEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shutdownWaitArmed = new LoopTickSignal();
        await using var coordinator = new MailWakeDaemonCoordinator(
            renewalLossStore, dispatcher, _fileSystem, _database, timeProvider, s_fastPolicy)
        {
            AfterAdmissionTickAsync = ticks.HookAsync,
            AfterLeadershipEndedAsync = _ =>
            {
                leadershipEnded.TrySetResult();
                return Task.CompletedTask;
            },
            AfterShutdownWaitArmedAsync = shutdownWaitArmed.HookAsync
        };

        // act
        var ready = ticks.WaitForNextTickAsync(cancellationToken);
        await coordinator.StartAsync(cancellationToken);
        await ready;
        await dispatcher.Entered.Task.WaitAsync(s_hangGuard, cancellationToken);
        var shutdownWaitArmedWait = shutdownWaitArmed.WaitForNextTickAsync(cancellationToken);
        renewalLossStore.FailNextRenewal();
        timeProvider.Advance(s_fastPolicy.HeartbeatInterval);
        await leadershipEnded.Task.WaitAsync(s_hangGuard, cancellationToken);
        await shutdownWaitArmedWait;
        timeProvider.Advance(s_fastPolicy.ShutdownWait);
        await releaseStore.Released.WaitAsync(s_hangGuard, cancellationToken);
        var reacquired = await new MailWakeDaemonLeaderStore(_fileSystem, _database).TryAcquireAsync(
            "someone-else", timeProvider.GetUtcNow(), TimeSpan.FromSeconds(60), cancellationToken);

        // assert
        // Released within the shutdown budget even though the stuck dispatch never returned.
        Assert.Contains("shutdown", coordinator.Status.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.True(reacquired);

        // The stuck dispatch is released now so its orphaned task and the drain can both finish.
        dispatcher.Release();
        await coordinator.StopAsync(cancellationToken);
    }

    private MailWakeDaemonCoordinator CreateCoordinator(
        FakePingSessionExecutor executor,
        TimeProvider timeProvider,
        IMailWakeDaemonLeaderStore? leaderStore = null,
        LoopTickSignal? ticks = null,
        LoopTickSignal? retryDelayArmed = null)
        => new(
            leaderStore ?? new MailWakeDaemonLeaderStore(_fileSystem, _database),
            new ActorWakeDispatcher(_batches, _agentStore, _gateCoordinator, executor, _mail, timeProvider),
            _fileSystem,
            _database,
            timeProvider,
            s_fastPolicy)
        {
            AfterAdmissionTickAsync = ticks is null ? null : ticks.HookAsync,
            AfterStandbyTickAsync = ticks is null ? null : ticks.HookAsync,
            AfterRetryDelayArmedAsync = retryDelayArmed is null ? null : retryDelayArmed.HookAsync
        };

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

    private async Task InsertDueOutboxRowAsync(
        CancellationToken cancellationToken, FakeTimeProvider timeProvider, string actor = Actor)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_outbox (actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES (@actor, 1, 0, @now, @now)
            """;
        command.Parameters.AddWithValue("@actor", actor);
        command.Parameters.AddWithValue("@now", timeProvider.GetUtcNow());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Repeatedly advances <paramref name="timeProvider"/> by <paramref name="step"/> until
    /// <paramref name="condition"/> is observed true. Each iteration arms
    /// <paramref name="waitForNextTickAsync"/>'s next signal before advancing the clock, so the
    /// tick that advance triggers can never race ahead of the wait that observes it.
    /// </summary>
    private static async Task AdvanceUntilAsync(
        FakeTimeProvider timeProvider,
        TimeSpan step,
        Func<CancellationToken, Task> waitForNextTickAsync,
        Func<bool> condition,
        CancellationToken cancellationToken)
    {
        while (!condition())
        {
            var tick = waitForNextTickAsync(cancellationToken);
            timeProvider.Advance(step);
            await tick;
        }
    }

    private static async Task AdvanceUntilAsync(
        FakeTimeProvider timeProvider,
        TimeSpan step,
        Func<CancellationToken, Task> waitForNextTickAsync,
        Func<Task<bool>> condition,
        CancellationToken cancellationToken)
    {
        while (!await condition())
        {
            var tick = waitForNextTickAsync(cancellationToken);
            timeProvider.Advance(step);
            await tick;
        }
    }
}

/// <summary>
/// A repeatable signal for a coordinator loop hook (<c>AfterAdmissionTickAsync</c> or
/// <c>AfterStandbyTickAsync</c>): each call to <see cref="WaitForNextTickAsync"/> reads the
/// currently armed completion source, which <see cref="HookAsync"/> fulfills the next time the
/// loop invokes it. Callers must call <see cref="WaitForNextTickAsync"/> before whatever action
/// triggers the next tick, so the wait cannot race a tick that fires before it is armed. Bounded
/// by a generous real-time hang guard so a genuinely broken test fails instead of hanging.
/// </summary>
internal sealed class LoopTickSignal
{
    private static readonly TimeSpan s_hangGuard = TimeSpan.FromSeconds(60);

    private TaskCompletionSource _next = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task HookAsync(CancellationToken cancellationToken)
    {
        var completed = Interlocked.Exchange(
            ref _next, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        completed.TrySetResult();
        return Task.CompletedTask;
    }

    public Task WaitForNextTickAsync(CancellationToken cancellationToken)
        => Volatile.Read(ref _next).Task.WaitAsync(s_hangGuard, cancellationToken);
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except the first
/// <see cref="TryAcquireAsync"/> call, which throws a non-busy
/// <see cref="SqliteException"/> instead and signals <see cref="Faulted"/> beforehand.
/// </summary>
internal sealed class FaultingLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private readonly TaskCompletionSource _faulted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _acquireCalls;

    public Task Faulted => _faulted.Task;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _acquireCalls) == 1)
        {
            _faulted.TrySetResult();
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
/// throw a <see cref="SqliteException"/> for SQLITE_BUSY instead. Each attempt
/// (busy or not) signals <see cref="WaitForNextCallAsync"/>'s next tick.
/// </summary>
internal sealed class BusyLeaderStore(IMailWakeDaemonLeaderStore inner, int busyAcquireCalls)
    : IMailWakeDaemonLeaderStore
{
    private readonly LoopTickSignal _calls = new();
    private int _acquireCalls;

    public int AcquireCalls => Volatile.Read(ref _acquireCalls);

    public Task WaitForNextCallAsync(CancellationToken cancellationToken) => _calls.WaitForNextTickAsync(cancellationToken);

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        var call = Interlocked.Increment(ref _acquireCalls);
        _ = _calls.HookAsync(cancellationToken);

        if (call <= busyAcquireCalls)
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
/// as <c>"release-attempted"</c> and signals <see cref="WaitForNextReleaseAttemptAsync"/>'s
/// next tick, for asserting ordering against other recorded events.
/// </summary>
internal sealed class BusyReleaseLeaderStore(
    IMailWakeDaemonLeaderStore inner, int busyReleaseCalls, ConcurrentQueue<string>? events = null)
    : IMailWakeDaemonLeaderStore
{
    private readonly LoopTickSignal _releaseAttempts = new();
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _releaseCalls;

    public int ReleaseCalls => Volatile.Read(ref _releaseCalls);

    /// <summary>
    /// Resolves once a non-busy <see cref="TryReleaseAsync"/> call has actually reached the
    /// database and returned, unlike <see cref="WaitForNextReleaseAttemptAsync"/> which fires
    /// on entry to every attempt, busy or not.
    /// </summary>
    public Task Released => _released.Task;

    public Task WaitForNextReleaseAttemptAsync(CancellationToken cancellationToken)
        => _releaseAttempts.WaitForNextTickAsync(cancellationToken);

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public async Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        events?.Enqueue("release-attempted");
        var call = Interlocked.Increment(ref _releaseCalls);
        _ = _releaseAttempts.HookAsync(cancellationToken);

        if (call <= busyReleaseCalls)
        {
            throw new SqliteException("busy", 5); // SQLITE_BUSY
        }

        var released = await inner.TryReleaseAsync(token, now, cancellationToken);
        _released.TrySetResult();
        return released;
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
/// Every <see cref="DispatchAsync"/> call blocks after being counted, once
/// <paramref name="capacity"/> concurrent calls are observed, and completes
/// <see cref="CapacityReached"/>. Every call unblocks together once
/// <see cref="ReleaseAll"/> is called.
/// </summary>
internal sealed class ConcurrencyTrackingDispatcher(int capacity) : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _capacityReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _concurrent;
    private int _maxObservedConcurrency;

    public int MaxObservedConcurrency => Volatile.Read(ref _maxObservedConcurrency);

    public ConcurrentBag<string> CompletedActors { get; } = [];

    public Task CapacityReached => _capacityReached.Task;

    public void ReleaseAll() => _released.TrySetResult();

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var observed = Interlocked.Increment(ref _concurrent);
        InterlockedMax(ref _maxObservedConcurrency, observed);

        if (observed >= capacity)
        {
            _capacityReached.TrySetResult();
        }

        await _released.Task;
        Interlocked.Decrement(ref _concurrent);
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
/// Records each distinct actor's entry and completes <see cref="AllEntered"/> once
/// <paramref name="expectedActors"/> distinct actors have entered, then hangs every
/// call until its own <see cref="CancellationToken"/> fires.
/// </summary>
internal sealed class ConcurrentEntryDispatcher(int expectedActors) : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentDictionary<string, bool> _entered = new(StringComparer.Ordinal);

    public Task AllEntered => _allEntered.Task;

    public int EnteredCount => _entered.Count;

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        if (_entered.TryAdd(actor, true) && _entered.Count >= expectedActors)
        {
            _allEntered.TrySetResult();
        }

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
/// Blocks every <see cref="DispatchAsync"/> call on an internal gate until
/// <see cref="Release"/> is called, ignoring the call's own cancellation
/// token. Signals <see cref="Entered"/> once a call has entered.
/// </summary>
internal sealed class HangingDispatcher : IActorWakeDispatcher
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Release() => _gate.TrySetResult();

    public async Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        Entered.TrySetResult();
        await _gate.Task;
        return null;
    }
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that once
/// <see cref="FailNextRenewal"/> has been called, every subsequent
/// <see cref="TryRenewAsync"/> call returns false without reaching
/// <paramref name="inner"/>. Callers that need to know when the coordinator has
/// actually processed the rejection should synchronize through one of its own
/// hooks (for example <c>AfterLeadershipEndedAsync</c>), not through this store:
/// a signal raised here fires before the coordinator's own state update, not after it.
/// </summary>
internal sealed class RenewalLossLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private volatile bool _failRenewal;

    public void FailNextRenewal() => _failRenewal = true;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => _failRenewal ? Task.FromResult(false) : inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that once
/// <see cref="FailNextRenewal"/> has been called, the next <see cref="TryRenewAsync"/>
/// call throws instead of renewing, signaling <see cref="RenewalFaulted"/> beforehand.
/// </summary>
internal sealed class ThrowingRenewalLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private readonly TaskCompletionSource _renewalFaulted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _failRenewal;

    public Task RenewalFaulted => _renewalFaulted.Task;

    public void FailNextRenewal() => _failRenewal = true;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (_failRenewal)
        {
            _renewalFaulted.TrySetResult();
            throw new InvalidOperationException("Simulated renewal fault.");
        }

        return inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);
    }

    public Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(token, now, cancellationToken);
}

/// <summary>
/// Delegates every call to <paramref name="inner"/> and completes <see cref="Released"/>
/// once <see cref="TryReleaseAsync"/> has been called and has returned. Every
/// <see cref="TryAcquireAsync"/> call made after that fails without reaching
/// <paramref name="inner"/>, so the same coordinator instance cannot race a test's own
/// direct acquire attempt for the lease it just gave up.
/// </summary>
internal sealed class ReleaseSignalLeaderStore(IMailWakeDaemonLeaderStore inner) : IMailWakeDaemonLeaderStore
{
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Released => _released.Task;

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => _released.Task.IsCompleted
            ? Task.FromResult(false)
            : inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryRenewAsync(token, now, leaseDuration, cancellationToken);

    public async Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var released = await inner.TryReleaseAsync(token, now, cancellationToken);
        _released.TrySetResult();
        return released;
    }
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

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except that after
/// <see cref="HoldNextRenewal"/> the next <see cref="TryRenewAsync"/> call marks
/// <see cref="HeldEntered"/> and awaits <see cref="ReleaseHeldRenewal"/> before completing
/// against <paramref name="inner"/> with no cancellation, then records
/// <c>"renewal-completed"</c> into <paramref name="events"/>. Every
/// <see cref="TryReleaseAsync"/> call records <c>"release-attempted"</c> before delegating
/// and signals <see cref="Released"/> once it returns.
/// </summary>
internal sealed class OrderedReleaseLeaderStore(IMailWakeDaemonLeaderStore inner, ConcurrentQueue<string> events)
    : IMailWakeDaemonLeaderStore
{
    private readonly TaskCompletionSource _heldEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? _gate;

    public Task HeldEntered => _heldEntered.Task;

    public Task Released => _released.Task;

    public void HoldNextRenewal() => _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void ReleaseHeldRenewal() => _gate?.TrySetResult();

    public Task<bool> TryAcquireAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
        => inner.TryAcquireAsync(token, now, leaseDuration, cancellationToken);

    public async Task<bool> TryRenewAsync(string token, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        if (_gate is { } gate)
        {
            _heldEntered.TrySetResult();
            await gate.Task;
        }

        // Simulates a renewal already in flight against the database: it runs to
        // completion rather than being cut short by the caller's own cancellation.
        var renewed = await inner.TryRenewAsync(token, now, leaseDuration, CancellationToken.None);
        events.Enqueue("renewal-completed");
        return renewed;
    }

    public async Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        events.Enqueue("release-attempted");
        var released = await inner.TryReleaseAsync(token, now, cancellationToken);
        _released.TrySetResult();
        return released;
    }
}

/// <summary>
/// Returns a pending, access-denied receipt for whichever actor it is dispatched for,
/// on every call, without touching any real session, mail, or dispatch machinery.
/// </summary>
internal sealed class AlwaysDeniedDispatcher : IActorWakeDispatcher
{
    public Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
        => Task.FromResult<ActorWakeReceipt?>(new ActorWakeReceipt(
            actor,
            "denied",
            [new ActorWakeTargetReceipt(actor, MailWakeTargetStatus.Pending, null, null, "access-denied")]));
}
