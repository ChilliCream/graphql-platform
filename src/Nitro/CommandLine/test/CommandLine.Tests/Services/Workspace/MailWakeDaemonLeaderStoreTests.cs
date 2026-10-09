using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeDaemonLeaderStore"/>'s leader election over
/// the single persistent <c>mail_wake_daemons</c> row, directly against a
/// real workspace database.
/// </summary>
public sealed class MailWakeDaemonLeaderStoreTests : IDisposable
{
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
    public async Task TryAcquireAsync_Should_ReturnTrue_When_NoLeaseRowExistsYet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var acquired = await _leader.TryAcquireAsync("token-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(acquired);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ReturnFalse_When_ALiveLeaseIsAlreadyHeld()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        // even the same holder cannot re-acquire; it must renew instead.
        var acquired = await _leader.TryAcquireAsync("token-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.False(acquired);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_Succeed_When_StealingAnExpiredLease()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", acquiredAt, TimeSpan.FromSeconds(30), cancellationToken);
        var later = acquiredAt + TimeSpan.FromSeconds(31);

        // act
        var acquired = await _leader.TryAcquireAsync("token-2", later, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(acquired);
        var renewedByOriginal = await _leader.TryRenewAsync(
            "token-1", later, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.False(renewedByOriginal);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ExtendTheLease_When_TokenStillHolds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var justBeforeExpiry = acquiredAt + TimeSpan.FromSeconds(9);

        // act
        var renewed = await _leader.TryRenewAsync(
            "token-1", justBeforeExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.True(renewed);
        var stolen = await _leader.TryAcquireAsync(
            "token-2", acquiredAt + TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10), cancellationToken);
        Assert.False(stolen);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_TokenDoesNotHoldTheLease()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var acquiredAt = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var later = acquiredAt + TimeSpan.FromSeconds(11);
        await _leader.TryAcquireAsync("token-2", later, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        // the stale holder tries to renew after a new holder has taken over.
        var renewed = await _leader.TryRenewAsync("token-1", later, TimeSpan.FromSeconds(10), cancellationToken);

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
        await _leader.TryAcquireAsync("token-1", acquiredAt, TimeSpan.FromSeconds(10), cancellationToken);
        var afterExpiry = acquiredAt + TimeSpan.FromSeconds(11);

        // act
        var renewed = await _leader.TryRenewAsync("token-1", afterExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_ExpireTheLeaseImmediately_When_TokenHolds()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var released = await _leader.TryReleaseAsync("token-1", now, cancellationToken);
        var reacquired = await _leader.TryAcquireAsync("token-2", now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(released);
        Assert.True(reacquired);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_ReturnFalse_When_TokenDoesNotHold()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await _leader.TryAcquireAsync("token-1", now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var released = await _leader.TryReleaseAsync("token-2", now, cancellationToken);

        // assert
        Assert.False(released);
    }

    [Fact]
    public async Task TryAcquireAsync_Should_ElectExactlyOneHolder_When_SixDifferentTokensRaceTheSameLease()
    {
        // arrange
        // separate connections (Pooling=False, matching production) racing with six distinct tokens.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var results = await ConcurrentTestHarness.RunAsync(
            6,
            i => new MailWakeDaemonLeaderStore(_fileSystem, _database)
                .TryAcquireAsync($"token-{i}", now, TimeSpan.FromSeconds(30), cancellationToken));

        // assert
        // exactly one distinct token ever won, so two tokens never both lead.
        Assert.Single(results, acquired => acquired);
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        await using (await _database.InitializeAsync(_workspaceDirectory, cancellationToken))
        {
        }
    }
}
