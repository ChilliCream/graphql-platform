using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeDaemonLeaderStore"/>'s leader election over
/// the one persistent <c>mail_wake_daemons</c> row per Nitro instance
/// directly against a real workspace database: first acquisition, a live
/// lease rejecting a second claimant, epoch monotonically incrementing every
/// time leadership changes hands, renewal fenced by owner and epoch,
/// voluntary release freeing the lease immediately, and exactly one leader
/// winning when several processes race the same instance concurrently.
/// </summary>
public sealed class MailWakeDaemonLeaderStoreTests : IDisposable
{
    private const string InstanceId = "instance-a";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly MailWakeDaemonLeaderStore _leader;

    public MailWakeDaemonLeaderStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-daemon-leader-store-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _leader = new MailWakeDaemonLeaderStore(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task TryAcquireAsync_Should_ReturnEpochOne_When_NoLeaderRowExistsYet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var epoch = await _leader.TryAcquireAsync(InstanceId, "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Equal(1, epoch);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ReturnNull_When_ALiveLeaseIsAlreadyHeld()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync(InstanceId, "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act: even the same owner cannot re-acquire while its own lease is
        // still live; renewal is TryRenewAsync's job.
        var epoch = await _leader.TryAcquireAsync(InstanceId, "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Null(epoch);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_IncrementEpoch_When_StealingAnExpiredLease()
    {
        // arrange: one leader epoch across successive owners.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var firstEpoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-1", acquiredAt, TimeSpan.FromSeconds(30), cancellationToken);
        var later = acquiredAt + TimeSpan.FromSeconds(31);

        // act
        var secondEpoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-2", later, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Equal(1, firstEpoch);
        Assert.Equal(2, secondEpoch);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ExtendTheLease_When_OwnerAndEpochStillMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var epoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var justBeforeExpiry = acquiredAt + TimeSpan.FromSeconds(9);

        // act
        var renewed = await _leader.TryRenewAsync(
            InstanceId, "owner-1", epoch!.Value, justBeforeExpiry, TimeSpan.FromSeconds(10), null, cancellationToken);

        // assert: a rival trying to steal right after the original lease
        // would have expired now fails, proving the renewal took effect.
        Assert.True(renewed);
        var stolen = await _leader.TryAcquireAsync(
            InstanceId, "owner-2", acquiredAt + TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10), cancellationToken);
        Assert.Null(stolen);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_EpochNoLongerMatches()
    {
        // arrange: fenced stale writes - an owner that lost leadership to a
        // fresher epoch can never renew its old one back to life.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var staleEpoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var later = acquiredAt + TimeSpan.FromSeconds(11);
        await _leader.TryAcquireAsync(InstanceId, "owner-2", later, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var renewed = await _leader.TryRenewAsync(
            InstanceId, "owner-1", staleEpoch!.Value, later, TimeSpan.FromSeconds(10), null, cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_LeaseHasAlreadyExpired()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var epoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var afterExpiry = acquiredAt + TimeSpan.FromSeconds(11);

        // act
        var renewed = await _leader.TryRenewAsync(
            InstanceId, "owner-1", epoch!.Value, afterExpiry, TimeSpan.FromSeconds(10), null, cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_ExpireTheLeaseImmediately_When_OwnerAndEpochMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var epoch = await _leader.TryAcquireAsync(InstanceId, "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var released = await _leader.TryReleaseAsync(InstanceId, "owner-1", epoch!.Value, now, cancellationToken);
        var reacquiredEpoch =
            await _leader.TryAcquireAsync(InstanceId, "owner-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert: the next acquirer, right away, without waiting out the
        // lease duration, gets a fresh epoch.
        Assert.True(released);
        Assert.Equal(2, reacquiredEpoch);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_ReturnFalse_When_EpochDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync(InstanceId, "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var released = await _leader.TryReleaseAsync(InstanceId, "owner-1", 999, now, cancellationToken);

        // assert
        Assert.False(released);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ElectExactlyOneLeader_When_SixConcurrentProcessesRaceTheSameInstance()
    {
        // arrange: separate connections (Pooling=False, matching production)
        // racing the same file for the same instance.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var results = await Task.WhenAll(Enumerable.Range(1, 6).Select(i =>
            new MailWakeDaemonLeaderStore(_fileSystem, _database)
                .TryAcquireAsync(InstanceId, $"owner-{i}", now, TimeSpan.FromSeconds(30), cancellationToken)));

        // assert: exactly one caller became leader, with epoch 1.
        var won = results.Where(epoch => epoch is not null).ToArray();
        Assert.Single(won);
        Assert.Equal(1, won[0]);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ElectIndependentLeaders_When_InstanceIdsDiffer()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var epochA = await _leader.TryAcquireAsync("instance-a", "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);
        var epochB = await _leader.TryAcquireAsync("instance-b", "owner-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert: a live lease on one instance never blocks another.
        Assert.Equal(1, epochA);
        Assert.Equal(1, epochB);
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }
}
