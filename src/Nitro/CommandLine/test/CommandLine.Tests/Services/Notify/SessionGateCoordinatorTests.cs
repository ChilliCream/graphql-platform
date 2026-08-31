using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="SessionGateCoordinator"/> against real
/// <see cref="SessionPingGateStore"/> and <see cref="PingLeaseStore"/>
/// instances: reserving a free target holds both the gate and a lease slot
/// together, a busy gate and a full lease pool are distinguished, a
/// rejected capacity attempt never leaves the gate held, a successful
/// completion starts a cooldown, and a failed completion releases
/// immediately.
/// </summary>
public sealed class SessionGateCoordinatorTests : IDisposable
{
    private static readonly AgentSessionGeneration TargetA = new("claude-code", "session-1", "host-1");
    private static readonly AgentSessionGeneration TargetB = new("claude-code", "session-2", "host-1");

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly SessionPingGateStore _gates;
    private readonly PingLeaseStore _leases;
    private readonly SessionGateCoordinator _coordinator;

    public SessionGateCoordinatorTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-session-gate-coordinator-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _gates = new SessionPingGateStore(_fileSystem, _database);
        _leases = new PingLeaseStore(_fileSystem, _database);
        _coordinator = new SessionGateCoordinator(_gates, _leases);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task TryReserveAsync_Should_ReserveBothTheGateAndALeaseSlot_When_Free()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var result = await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // assert
        Assert.NotNull(result.Reservation);
        Assert.Null(result.Failure);
        Assert.Equal(TargetA, result.Reservation.Target);
        Assert.InRange(result.Reservation.Slot, 1, 4);
    }

    [Fact]
    public async Task TryReserveAsync_Should_ReturnGateBusy_When_TheGateIsAlreadyHeldByAnUnexpiredAttempt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // act: a second attempt against the exact same target generation.
        var result = await _coordinator.TryReserveAsync(TargetA, "attempt-2", now, cancellationToken);

        // assert
        Assert.Null(result.Reservation);
        Assert.Equal(WakeReservationFailure.GateBusy, result.Failure);
    }

    [Fact]
    public async Task TryReserveAsync_Should_ReturnCapacityDropped_And_ReleaseTheGate_When_EveryLeaseSlotIsHeld()
    {
        // arrange: every one of the four shared lease slots is already held
        // by unrelated attempts.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        for (var i = 1; i <= 4; i++)
        {
            await _leases.TryAcquireAsync($"holder-{i}", now, TimeSpan.FromSeconds(30), cancellationToken);
        }

        // act
        var result = await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // assert
        Assert.Null(result.Reservation);
        Assert.Equal(WakeReservationFailure.CapacityDropped, result.Failure);

        // the gate this attempt reserved before hitting capacity was
        // released again, so an immediate retry for the same target is not
        // blocked by a gate this rejected attempt never should have kept.
        var retryResult = await _coordinator.TryReserveAsync(TargetB, "attempt-2", now, cancellationToken);
        Assert.Null(retryResult.Reservation);
        Assert.Equal(WakeReservationFailure.CapacityDropped, retryResult.Failure);

        var gateStillHeld = await _gates.TryAcquireAsync(
            TargetA, "attempt-3", now, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.True(gateStillHeld);
    }

    [Fact]
    public async Task CompleteAsync_Should_StartACooldown_When_Successful()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var reserved = await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // act
        await _coordinator.CompleteAsync(reserved.Reservation!, success: true, now, cancellationToken);

        // assert: a fresh attempt against the same generation, immediately
        // after, finds the gate still busy (the cooldown), not free.
        var retry = await _coordinator.TryReserveAsync(TargetA, "attempt-2", now, cancellationToken);
        Assert.Null(retry.Reservation);
        Assert.Equal(WakeReservationFailure.GateBusy, retry.Failure);

        // and after the cooldown elapses, it is free again.
        var afterCooldown = await _coordinator.TryReserveAsync(
            TargetA, "attempt-3", now + PingPolicy.Cooldown + TimeSpan.FromSeconds(1), cancellationToken);
        Assert.NotNull(afterCooldown.Reservation);
    }

    [Fact]
    public async Task CompleteAsync_Should_ReleaseImmediately_When_Unsuccessful()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var reserved = await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // act
        await _coordinator.CompleteAsync(reserved.Reservation!, success: false, now, cancellationToken);

        // assert: no cooldown at all - a fresh attempt succeeds right away.
        var retry = await _coordinator.TryReserveAsync(TargetA, "attempt-2", now, cancellationToken);
        Assert.NotNull(retry.Reservation);
    }

    [Fact]
    public async Task CompleteAsync_Should_ReleaseTheLeaseSlot_Regardless()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var reserved = await _coordinator.TryReserveAsync(TargetA, "attempt-1", now, cancellationToken);

        // act
        await _coordinator.CompleteAsync(reserved.Reservation!, success: true, now, cancellationToken);

        // assert: all four slots are free again (only one was ever taken).
        var acquired = await Task.WhenAll(Enumerable.Range(1, 4).Select(i =>
            _leases.TryAcquireAsync($"probe-{i}", now, TimeSpan.FromSeconds(30), cancellationToken)));
        Assert.All(acquired, slot => Assert.NotNull(slot));
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }
}
