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

    private async Task InsertDueOutboxRowAsync(string nitroInstanceId, CancellationToken cancellationToken)
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
        command.Parameters.AddWithValue("@actor", Actor);
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

/// <summary>
/// Delegates every call to <paramref name="inner"/>, except the first
/// <see cref="TryAcquireAsync"/> call, which throws a non-busy
/// <see cref="SqliteException"/> instead: a stand-in for an unexpected
/// infrastructure fault on the election path that is not the store's own
/// busy/locked retry signal.
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
/// Delegates every call to <paramref name="inner"/>, except that once
/// <see cref="FailNextRenewal"/> has been called, every subsequent
/// <see cref="TryRenewAsync"/> call returns false without reaching
/// <paramref name="inner"/> at all: a stand-in for the current lease having
/// been lost to a fresher claimant.
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
