using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;

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
        await coordinator.StartAsync(InstanceId, cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Ready, cancellationToken);

        // assert
        Assert.Equal(1, coordinator.Status.Epoch);
        Assert.Equal(coordinator.Status.OwnerId, coordinator.Status.OwnerId);

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
        await coordinator.StartAsync(InstanceId, cancellationToken);
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
        await coordinator.StartAsync(InstanceId, cancellationToken);
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
        await coordinatorA.StartAsync(InstanceId, cancellationToken);
        await coordinatorB.StartAsync(InstanceId, cancellationToken);
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
        await coordinator.StartAsync(InstanceId, cancellationToken);
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
        await coordinator.StartAsync(InstanceId, cancellationToken);
        await WaitUntilAsync(() => coordinator.Status.State == MailWakeDaemonState.Degraded, cancellationToken);

        // assert: degraded with the denial recorded, and it does not flap
        // back to ready on its own within a few more poll cycles.
        Assert.Equal("access-denied", coordinator.Status.LastError);
        await Task.Delay(FastPolicy.StandbyPollInterval * 5, cancellationToken);
        Assert.NotEqual(MailWakeDaemonState.Ready, coordinator.Status.State);

        // a differently privileged standby (a second coordinator instance)
        // can take over immediately, without waiting out the lease.
        await using var standby = CreateCoordinator(new FakePingSessionExecutor());
        await standby.StartAsync(InstanceId, cancellationToken);
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
        await coordinator.StartAsync(InstanceId, cancellationToken);
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

    private MailWakeDaemonCoordinator CreateCoordinator(FakePingSessionExecutor executor)
        => new(
            new MailWakeDaemonLeaderStore(_fileSystem, _database),
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
            TimeProvider.System,
            FastPolicy);

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }

    private async Task<AgentSessionGeneration> SeedLiveSessionAsync(
        string endpointKind, string endpointAddr, CancellationToken cancellationToken, string sessionId = "session-1")
    {
        var pid = Environment.ProcessId;
        var procStart = new ProcessInfoProvider().GetStartTicks(pid)!;
        var harness = endpointKind == AgentSessionEndpointKind.ClaudePeer
            ? AgentSessionHarness.ClaudeCode
            : AgentSessionHarness.Codex;
        var generation = new AgentSessionGeneration(harness, sessionId, InstanceId, pid, procStart);

        await _sessions.StartAsync(
            generation, "/work", "/work/.nitro/agents", endpointKind, endpointAddr, envActor: Actor, cancellationToken);

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

    private async Task<string?> ReadTargetStatusAsync(string sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await _database.ConnectAsync(_workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM mail_wake_targets WHERE session_id = @sessionId";
        command.Parameters.AddWithValue("@sessionId", sessionId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;

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
