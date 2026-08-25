using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="SessionPingGateStore"/>'s atomic
/// claim-or-steal-expired upsert directly against a real workspace database:
/// mutual exclusion for one exact session generation, stealing an expired
/// gate, renewal and its expiry fencing, release by attempt id, and the
/// exclusion holding across genuinely concurrent processes (separate
/// connections racing the same database file).
/// </summary>
public sealed class SessionPingGateStoreTests : IDisposable
{
    private static readonly AgentSessionGeneration Generation =
        new("claude-code", "session-1", "host-a", 4242, "123456");

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly SessionPingGateStore _gates;

    public SessionPingGateStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-session-ping-gate-store-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _gates = new SessionPingGateStore(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task TryAcquireAsync_Should_ClaimTheGate_When_NoneIsHeld()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var claimed = await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(claimed);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ReturnFalse_When_GateIsHeldAndUnexpired()
    {
        // arrange: mutual exclusion for one exact session generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var claimed = await _gates.TryAcquireAsync(Generation, "attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_AllowDifferentGenerations_When_SameSessionIdOnDifferentPid()
    {
        // arrange: a stale generation (an older pid the OS has since reused)
        // must never contend with the current one.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);
        var otherGeneration = Generation with { Pid = 9999, ProcStart = "999999" };

        // act
        var claimed =
            await _gates.TryAcquireAsync(otherGeneration, "attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(claimed);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_StealAnExpiredGate()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", acquiredAt, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var later = acquiredAt + TimeSpan.FromSeconds(31);
        var claimed = await _gates.TryAcquireAsync(Generation, "attempt-2", later, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(claimed);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ExtendExpiry_When_AttemptIdStillHoldsTheGate()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var justBeforeExpiry = acquiredAt + TimeSpan.FromSeconds(9);

        // act
        var renewed = await _gates.TryRenewAsync(
            Generation, "attempt-1", justBeforeExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert: a caller trying to steal right after the original lease
        // would have expired now fails, proving the renewal took effect.
        Assert.True(renewed);
        var stillHeld = await _gates.TryAcquireAsync(
            Generation, "attempt-2", acquiredAt + TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10), cancellationToken);
        Assert.False(stillHeld);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_LeaseHasAlreadyExpired()
    {
        // arrange: a lost gate can never be renewed back to life.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var afterExpiry = acquiredAt + TimeSpan.FromSeconds(11);

        // act
        var renewed =
            await _gates.TryRenewAsync(Generation, "attempt-1", afterExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_AttemptIdDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var renewed = await _gates.TryRenewAsync(Generation, "attempt-stale", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task ReleaseAsync_Should_FreeTheGate_When_AttemptIdMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        await _gates.ReleaseAsync(Generation, "attempt-1", cancellationToken);
        var reclaimed = await _gates.TryAcquireAsync(Generation, "attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(reclaimed);
    }

    [Fact]
    public async Task ReleaseAsync_Should_BeANoOp_When_AttemptIdDoesNotMatch()
    {
        // arrange: a late release from an attempt whose gate was already
        // stolen as expired (or already released) must never free a
        // DIFFERENT attempt's currently-held gate.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _gates.TryAcquireAsync(Generation, "attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        await _gates.ReleaseAsync(Generation, "attempt-stale", cancellationToken);
        var stillHeld = await _gates.TryAcquireAsync(Generation, "attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.False(stillHeld);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ClaimExactlyOnce_When_SixConcurrentProcessesRaceTheSameGeneration()
    {
        // arrange: separate connections (Pooling=False, matching production)
        // racing the same file for the same session generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var results = await Task.WhenAll(Enumerable.Range(1, 6).Select(i =>
            new SessionPingGateStore(_fileSystem, _database)
                .TryAcquireAsync(Generation, $"attempt-{i}", now, TimeSpan.FromSeconds(30), cancellationToken)));

        // assert: exactly one caller claimed the gate.
        Assert.Equal(1, results.Count(claimed => claimed));
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }
}
