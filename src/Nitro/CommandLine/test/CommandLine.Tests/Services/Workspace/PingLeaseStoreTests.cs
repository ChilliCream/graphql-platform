using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="PingLeaseStore"/>'s atomic insert-or-steal-expired
/// claim directly against a real workspace database: the fixed four-slot
/// cap, stealing an expired lease's slot, release by (slot, attempt_id),
/// and the cap holding across genuinely concurrent processes (separate
/// connections racing the same database file), one of the notifier's
/// required tests.
/// </summary>
public sealed class PingLeaseStoreTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly PingLeaseStore _leases;

    public PingLeaseStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-ping-lease-store-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _leases = new PingLeaseStore(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task TryAcquireAsync_Should_ClaimSlotOne_When_NoLeaseIsHeld()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var slot = await _leases.TryAcquireAsync("attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Equal(1, slot);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ReturnNull_When_AllFourSlotsAreHeldAndUnexpired()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        for (var i = 1; i <= 4; i++)
        {
            await _leases.TryAcquireAsync($"attempt-{i}", now, TimeSpan.FromSeconds(30), cancellationToken);
        }

        // act
        var slot = await _leases.TryAcquireAsync("attempt-5", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert: capacity-dropped, the caller's job to record.
        Assert.Null(slot);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_StealAnExpiredSlot()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        for (var i = 1; i <= 4; i++)
        {
            await _leases.TryAcquireAsync($"attempt-{i}", acquiredAt, TimeSpan.FromSeconds(30), cancellationToken);
        }

        // act: every slot's lease has now expired.
        var later = acquiredAt + TimeSpan.FromSeconds(31);
        var slot = await _leases.TryAcquireAsync("attempt-new", later, TimeSpan.FromSeconds(30), cancellationToken);

        // assert: a slot was reclaimed, not capacity-dropped.
        Assert.NotNull(slot);
        Assert.InRange(slot.Value, 1, 4);
    }

    [Fact]
    public async Task ReleaseAsync_Should_FreeTheSlot_When_AttemptIdMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var slot = await _leases.TryAcquireAsync("attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        await _leases.ReleaseAsync(slot!.Value, "attempt-1", cancellationToken);
        var reacquired = await _leases.TryAcquireAsync("attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Equal(slot, reacquired);
    }

    [Fact]
    public async Task ReleaseAsync_Should_BeANoOp_When_AttemptIdDoesNotMatch()
    {
        // arrange: a late release from an attempt whose lease was already
        // stolen as expired (or already released) must never free a
        // DIFFERENT attempt's currently-held slot.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var slot = await _leases.TryAcquireAsync("attempt-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        await _leases.ReleaseAsync(slot!.Value, "attempt-stale", cancellationToken);
        var stillHeld = await _leases.TryAcquireAsync("attempt-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert: slot 1 is still held by attempt-1, so the next acquire
        // (with 3 free slots remaining) lands on slot 2.
        Assert.Equal(2, stillHeld);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_CapAtExactlyFour_When_SixConcurrentProcessesRaceTheSameDatabase()
    {
        // arrange: separate connections (Pooling=False, matching production)
        // racing the same file - the notifier's required "cap holds across
        // concurrent processes" test.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var results = await Task.WhenAll(Enumerable.Range(1, 6).Select(i =>
            new PingLeaseStore(_fileSystem, _database)
                .TryAcquireAsync($"attempt-{i}", now, TimeSpan.FromSeconds(30), cancellationToken)));

        // assert: exactly four callers claimed a distinct slot, the other
        // two were capacity-dropped.
        var claimed = results.Where(slot => slot is not null).Select(slot => slot!.Value).ToArray();
        Assert.Equal(4, claimed.Length);
        Assert.Equal([1, 2, 3, 4], claimed.OrderBy(s => s).ToArray());
        Assert.Equal(2, results.Count(slot => slot is null));
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }
}
