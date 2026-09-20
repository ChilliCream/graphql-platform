using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="MailWakeBatchStore"/>'s atomic claim, renew,
/// complete, and release primitives directly against a real workspace
/// database.
/// </summary>
public sealed class MailWakeBatchStoreTests : IDisposable
{
    private const string InstanceId = "instance-a";
    private const string Actor = "claude";

    private static readonly AgentSessionGeneration s_target =
        new("claude-code", "session-1", "host-a");

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly TestFileSystem _fileSystem;
    private readonly AgentDatabase _database;
    private readonly MailWakeBatchStore _batches;

    public MailWakeBatchStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-wake-batch-store-tests");
        _workspaceDirectory = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(_workspaceDirectory);
        _fileSystem = new TestFileSystem(_tempRoot.FullName);
        _database = new AgentDatabase();
        _batches = new MailWakeBatchStore(_fileSystem, _database);
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task TryClaimAsync_Should_ReturnNull_When_NoOutboxRowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

        // act
        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Null(claim);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnNull_When_SettledGenerationAlreadyEqualsRequested()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(connection, requestedGeneration: 1, settledGeneration: 1, dueAt: now, cancellationToken);
        }

        // act
        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Null(claim);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnNull_When_DueAtIsInTheFuture()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(
                connection, requestedGeneration: 1, settledGeneration: 0, dueAt: now + TimeSpan.FromMinutes(1), cancellationToken);
        }

        // act
        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.Null(claim);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ClaimGenerationAndMaterializeTargets_When_WorkIsOutstandingAndDue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await using (var seedConnection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(seedConnection, cancellationToken);
            await SeedOutboxAsync(seedConnection, requestedGeneration: 3, settledGeneration: 1, dueAt: now, cancellationToken);
        }

        // act
        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.NotNull(claim);
        Assert.Equal(3, claim.ClaimedGeneration);
        Assert.Equal([s_target], claim.Targets);

        await using var connection = await ConnectAsync(cancellationToken);
        var targetCount = await ExecuteScalarLongAsync(
            connection,
            $"SELECT COUNT(*) FROM mail_wake_targets WHERE batch_id = '{claim.BatchId}'",
            cancellationToken);
        Assert.Equal(1, targetCount);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnNull_When_AnActiveBatchAlreadyExistsForTheActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(connection, requestedGeneration: 2, settledGeneration: 0, dueAt: now, cancellationToken);
        }
        var firstClaim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var secondClaim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-2", "attempt-2", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.NotNull(firstClaim);
        Assert.Null(secondClaim);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnNull_When_TheActiveBatchLeaseHasNotExpiredYet()
    {
        // arrange: a batch claimed with a 10s lease.
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var firstClaim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(10), cancellationToken);

        // act
        // Attempt a second claim five seconds into the ten-second lease.
        var secondClaim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-2", "attempt-2", [s_target], now + TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.NotNull(firstClaim);
        Assert.Null(secondClaim);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReclaimTheActor_When_TheActiveBatchLeaseHasExpired()
    {
        // arrange: a batch claimed with a 10s lease.
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var firstClaim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(10), cancellationToken);

        // act: a new owner claims 11s later, after the lease expired.
        var secondClaim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-2", "attempt-2", [s_target], now + TimeSpan.FromSeconds(11),
            TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.NotNull(secondClaim);
        Assert.NotEqual(firstClaim.BatchId, secondClaim.BatchId);
        await using var connection = await ConnectAsync(cancellationToken);
        var oldStatus = await ExecuteScalarStringAsync(
            connection, $"SELECT status FROM mail_wake_batches WHERE batch_id = '{firstClaim.BatchId}'", cancellationToken);
        Assert.Equal("released", oldStatus);
    }

    [Fact]
    public async Task TryCompleteAsync_Should_ReturnFalse_When_BatchWasReclaimedAfterExpiry()
    {
        // arrange: the original batch's lease expires and a new owner reclaims the actor.
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var firstClaim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(10), cancellationToken);
        await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-2", "attempt-2", [s_target], now + TimeSpan.FromSeconds(11),
            TimeSpan.FromSeconds(10), cancellationToken);

        // act: the stale owner tries to complete the batch it no longer holds.
        var completed = await _batches.TryCompleteAsync(
            firstClaim.BatchId, "owner-1", "attempt-1", now + TimeSpan.FromSeconds(12), cancellationToken);

        // assert
        Assert.False(completed);
        await using var connection = await ConnectAsync(cancellationToken);
        var settledGeneration = await ExecuteScalarLongAsync(
            connection,
            $"SELECT settled_generation FROM mail_wake_outbox WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}'",
            cancellationToken);
        Assert.Equal(0, settledGeneration);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ClaimExactlyOnce_When_ConcurrentCallersRaceTheSameActor()
    {
        // arrange: separate connections (Pooling=False, matching production) racing the same actor.
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(connection, requestedGeneration: 1, settledGeneration: 0, dueAt: now, cancellationToken);
        }

        // act
        var results = await ConcurrentTestHarness.RunAsync(
            6,
            i => new MailWakeBatchStore(_fileSystem, _database).TryClaimAsync(
                InstanceId, Actor, $"owner-{i}", $"attempt-{i}", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken));

        // assert: exactly one caller claimed the batch.
        Assert.Single(results, claim => claim is not null);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ExtendExpiry_When_OwnerAndAttemptStillMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(10), cancellationToken);
        var justBeforeExpiry = now + TimeSpan.FromSeconds(9);

        // act
        var renewed = await _batches.TryRenewAsync(
            claim.BatchId, "owner-1", "attempt-1", justBeforeExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.True(renewed);
        var completed = await _batches.TryCompleteAsync(
            claim.BatchId, "owner-1", "attempt-1", now + TimeSpan.FromSeconds(11), cancellationToken);
        Assert.True(completed);
    }

    [Fact]
    public async Task TryRenewAsync_Should_ReturnFalse_When_LeaseHasAlreadyExpired()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(10), cancellationToken);
        var afterExpiry = now + TimeSpan.FromSeconds(11);

        // act
        var renewed = await _batches.TryRenewAsync(
            claim.BatchId, "owner-1", "attempt-1", afterExpiry, TimeSpan.FromSeconds(10), cancellationToken);

        // assert
        Assert.False(renewed);
    }

    [Fact]
    public async Task TryCompleteAsync_Should_SettleTheClaimedGeneration_When_OwnerAndAttemptMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken, requestedGeneration: 2);

        // act
        var completed = await _batches.TryCompleteAsync(claim.BatchId, "owner-1", "attempt-1", now, cancellationToken);

        // assert
        Assert.True(completed);
        await using var connection = await ConnectAsync(cancellationToken);
        var settledGeneration = await ExecuteScalarLongAsync(
            connection,
            $"SELECT settled_generation FROM mail_wake_outbox WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}'",
            cancellationToken);
        Assert.Equal(2, settledGeneration);
        var status = await ExecuteScalarStringAsync(
            connection, $"SELECT status FROM mail_wake_batches WHERE batch_id = '{claim.BatchId}'", cancellationToken);
        Assert.Equal("completed", status);
    }

    [Fact]
    public async Task TryCompleteAsync_Should_NotSettlePastTheClaimedGeneration_When_ANewerGenerationWasRequestedDuringTheBatch()
    {
        // arrange
        // Claim generation 1, then set the requested generation to 2.
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken, requestedGeneration: 1);

        await using (var connection = await ConnectAsync(cancellationToken))
        {
            await ExecuteAsync(
                connection,
                "UPDATE mail_wake_outbox SET requested_generation = 2 "
                + $"WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}';",
                cancellationToken);
        }

        // act
        var completed = await _batches.TryCompleteAsync(claim.BatchId, "owner-1", "attempt-1", now, cancellationToken);

        // assert
        Assert.True(completed);
        await using var assertConnection = await ConnectAsync(cancellationToken);
        var settledGeneration = await ExecuteScalarLongAsync(
            assertConnection,
            $"SELECT settled_generation FROM mail_wake_outbox WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}'",
            cancellationToken);
        Assert.Equal(1, settledGeneration);
    }

    [Fact]
    public async Task TryCompleteAsync_Should_ReturnFalse_When_AttemptIdDoesNotMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var completed = await _batches.TryCompleteAsync(claim.BatchId, "owner-1", "attempt-stale", now, cancellationToken);

        // assert
        Assert.False(completed);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_FreeTheActorForANewClaim_When_OwnerAndAttemptMatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var released = await _batches.TryReleaseAsync(
            claim.BatchId, "owner-1", "attempt-1", now, retryAt: null, lastError: "spawn-failed", cancellationToken);
        var reclaimed = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-2", "attempt-2", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.True(released);
        Assert.NotNull(reclaimed);
    }

    [Fact]
    public async Task TryReleaseAsync_Should_PushDueAtForward_When_RetryAtIsGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);
        var retryAt = now + TimeSpan.FromMinutes(1);

        // act
        await _batches.TryReleaseAsync(claim.BatchId, "owner-1", "attempt-1", now, retryAt, null, cancellationToken);

        // assert
        await using var connection = await ConnectAsync(cancellationToken);
        var dueAt = await ExecuteScalarStringAsync(
            connection,
            $"SELECT due_at FROM mail_wake_outbox WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}'",
            cancellationToken);
        Assert.Equal(retryAt, DateTimeOffset.Parse(dueAt!, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task TryReleaseAsync_Should_LeaveDueAtUnchanged_When_RetryAtIsNull()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        await _batches.TryReleaseAsync(
            claim.BatchId, "owner-1", "attempt-1", now, retryAt: null, lastError: "capacity-dropped", cancellationToken);

        // assert
        await using var connection = await ConnectAsync(cancellationToken);
        var dueAt = await ExecuteScalarStringAsync(
            connection,
            $"SELECT due_at FROM mail_wake_outbox WHERE nitro_instance_id = '{InstanceId}' AND actor = '{Actor}'",
            cancellationToken);
        Assert.Equal(now, DateTimeOffset.Parse(dueAt!, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task TryRecordTargetOutcomeAsync_Should_UpdateTheTargetRow_When_BatchIsActiveAndOwnerMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var recorded = await _batches.TryRecordTargetOutcomeAsync(
            claim.BatchId, s_target, "owner-1", "attempt-1", "delivered",
            offeredGeneration: null, acceptedGeneration: 1, lastError: null, now, cancellationToken);

        // assert
        Assert.True(recorded);
        await using var connection = await ConnectAsync(cancellationToken);
        var status = await ExecuteScalarStringAsync(
            connection, $"SELECT status FROM mail_wake_targets WHERE batch_id = '{claim.BatchId}'", cancellationToken);
        var acceptedGeneration = await ExecuteScalarLongAsync(
            connection, $"SELECT accepted_generation FROM mail_wake_targets WHERE batch_id = '{claim.BatchId}'", cancellationToken);
        Assert.Equal("delivered", status);
        Assert.Equal(1, acceptedGeneration);
    }

    [Fact]
    public async Task TryRecordTargetOutcomeAsync_Should_ReturnFalse_When_AttemptIdDoesNotMatchTheOwningBatch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var claim = await SeedClaimedBatchAsync(now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var recorded = await _batches.TryRecordTargetOutcomeAsync(
            claim.BatchId, s_target, "owner-1", "attempt-stale", "delivered",
            offeredGeneration: null, acceptedGeneration: 1, lastError: null, now, cancellationToken);

        // assert
        Assert.False(recorded);
    }

    [Fact]
    public async Task TryClaimAsync_Should_YieldAnIndependentClaim_When_ADifferentNitroInstanceClaimsTheSameActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        const string otherInstanceId = "instance-b";
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(connection, requestedGeneration: 1, settledGeneration: 0, dueAt: now, cancellationToken);
            await SeedOutboxAsync(
                connection, requestedGeneration: 1, settledGeneration: 0, dueAt: now, cancellationToken,
                instanceId: otherInstanceId);
        }
        var claimA = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-a", "attempt-a", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // act
        var claimB = await _batches.TryClaimAsync(
            otherInstanceId, Actor, "owner-b", "attempt-b", [s_target], now, TimeSpan.FromSeconds(30), cancellationToken);

        // assert
        Assert.NotNull(claimA);
        Assert.NotNull(claimB);
        Assert.NotEqual(claimA.BatchId, claimB.BatchId);

        var crossRenewed = await _batches.TryRenewAsync(
            claimA.BatchId, "owner-b", "attempt-b", now, TimeSpan.FromSeconds(30), cancellationToken);
        Assert.False(crossRenewed);

        var crossCompleted = await _batches.TryCompleteAsync(
            claimA.BatchId, "owner-b", "attempt-b", now, cancellationToken);
        Assert.False(crossCompleted);

        // instance-a's own owner still completes its own batch normally.
        var completedA = await _batches.TryCompleteAsync(claimA.BatchId, "owner-a", "attempt-a", now, cancellationToken);
        Assert.True(completedA);
    }

    private async Task<MailWakeBatchClaim> SeedClaimedBatchAsync(
        DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken, long requestedGeneration = 1)
    {
        await using (var connection = await InitializeWorkspaceAsync(cancellationToken))
        {
            await SeedActorAsync(connection, cancellationToken);
            await SeedOutboxAsync(connection, requestedGeneration, settledGeneration: 0, dueAt: now, cancellationToken);
        }

        var claim = await _batches.TryClaimAsync(
            InstanceId, Actor, "owner-1", "attempt-1", [s_target], now, leaseDuration, cancellationToken);

        return claim ?? throw new InvalidOperationException("Failed to seed a claimed batch for the test.");
    }

    private async Task<SqliteConnection> InitializeWorkspaceAsync(CancellationToken cancellationToken)
        => await _database.InitializeAsync(_workspaceDirectory, cancellationToken);

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
        => await _database.ConnectAsync(_workspaceDirectory, cancellationToken);

    private static async Task SeedActorAsync(SqliteConnection connection, CancellationToken cancellationToken)
        => await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, last_seen_at) VALUES "
            + $"('{Actor}', '2026-01-10T12:00:00+00:00', '2026-01-10T12:00:00+00:00');",
            cancellationToken);

    private static async Task SeedOutboxAsync(
        SqliteConnection connection,
        long requestedGeneration,
        long settledGeneration,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken,
        string instanceId = InstanceId)
    {
        // Bind dueAt with the same timestamp representation used by the store.
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO mail_wake_outbox (nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at)
            VALUES (@instanceId, @actor, @requestedGeneration, @settledGeneration, @dueAt, @dueAt);
            """;
        command.Parameters.AddWithValue("@instanceId", instanceId);
        command.Parameters.AddWithValue("@actor", Actor);
        command.Parameters.AddWithValue("@requestedGeneration", requestedGeneration);
        command.Parameters.AddWithValue("@settledGeneration", settledGeneration);
        command.Parameters.AddWithValue("@dueAt", dueAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> ExecuteScalarLongAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static async Task<string?> ExecuteScalarStringAsync(
        SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : result.ToString();
    }
}
