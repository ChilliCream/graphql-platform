using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore.Postgres;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Inbox;
using Mocha.Middlewares;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresMessageInboxTests : IClassFixture<PostgresFixture>
{
    private const string TestConsumerType = "MyApp.OrderPlacedHandler";
    private const string AltConsumerType = "MyApp.NotificationHandler";

    private readonly PostgresFixture _fixture;

    public PostgresMessageInboxTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_When_MessageNotRecorded()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        // Act
        var exists = await inbox.ExistsAsync("non-existent-id", TestConsumerType, CancellationToken.None);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task RecordAsync_Should_InsertRow_When_Called()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();

        // Act
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Assert
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM \"inbox_messages\"";
        var count = (long)(await cmd.ExecuteScalarAsync(CancellationToken.None))!;

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnTrue_When_MessageRecorded()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act
        var exists = await inbox.ExistsAsync(envelope.MessageId!, TestConsumerType, CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_When_DifferentConsumerType()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act — check with a different consumer type
        var exists = await inbox.ExistsAsync(envelope.MessageId!, AltConsumerType, CancellationToken.None);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task RecordAsync_Should_NotThrow_When_DuplicateMessageInserted()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act & Assert — ON CONFLICT DO NOTHING should prevent errors
        var ex = await Record.ExceptionAsync(() =>
            inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None).AsTask());

        Assert.Null(ex);
    }

    [Fact]
    public async Task RecordAsync_Should_AllowSameMessageForDifferentConsumers_When_Called()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();

        // Act — record same message for two different consumer types
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);
        await inbox.RecordAsync(envelope, AltConsumerType, CancellationToken.None);

        // Assert — both should exist
        var existsFirst = await inbox.ExistsAsync(envelope.MessageId!, TestConsumerType, CancellationToken.None);
        var existsSecond = await inbox.ExistsAsync(envelope.MessageId!, AltConsumerType, CancellationToken.None);
        Assert.True(existsFirst);
        Assert.True(existsSecond);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnTrue_When_MessageNotYetClaimed()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();

        // Act
        var claimed = await inbox.TryClaimAsync(envelope, TestConsumerType, CancellationToken.None);

        // Assert
        Assert.True(claimed);
        var exists = await inbox.ExistsAsync(envelope.MessageId!, TestConsumerType, CancellationToken.None);
        Assert.True(exists);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnFalse_When_MessageAlreadyClaimed()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.TryClaimAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act — second claim attempt for the same message ID and consumer type
        var claimed = await inbox.TryClaimAsync(envelope, TestConsumerType, CancellationToken.None);

        // Assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnTrue_When_DifferentConsumerType()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.TryClaimAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act — claim the same message with a different consumer type
        var claimed = await inbox.TryClaimAsync(envelope, AltConsumerType, CancellationToken.None);

        // Assert — should succeed because each consumer type claims independently
        Assert.True(claimed);
    }

    [Fact]
    public async Task TryClaimAsync_Should_ReturnFalse_When_MessageAlreadyRecorded()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act — try to claim a message that was already recorded via RecordAsync
        var claimed = await inbox.TryClaimAsync(envelope, TestConsumerType, CancellationToken.None);

        // Assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task CleanupAsync_Should_DeleteOldMessages_When_Called()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Backdate the processed_at so the cleanup finds it
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        await using var backdateCmd = connection.CreateCommand();
        backdateCmd.CommandText =
            "UPDATE \"inbox_messages\" SET \"processed_at\" = NOW() - INTERVAL '30 days'";
        await backdateCmd.ExecuteNonQueryAsync(CancellationToken.None);

        // Act
        var deleted = await inbox.CleanupAsync(TimeSpan.FromDays(7), CancellationToken.None);

        // Assert
        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task CleanupAsync_Should_NotDeleteRecentMessages_When_Called()
    {
        // Arrange
        var (context, inbox) = await CreateInboxAsync();
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act — message was just inserted, so it should not be cleaned up
        var deleted = await inbox.CleanupAsync(TimeSpan.FromDays(7), CancellationToken.None);

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task CleanupAsync_Should_DeleteMessages_When_TimeProviderAdvancedPastRetention()
    {
        // Arrange — start the fake clock at "now" and insert a message
        var baseTime = DateTimeOffset.UtcNow;
        var fakeTime = new FakeTimeProvider(baseTime);
        var (context, inbox) = await CreateInboxAsync(fakeTime);
        await using var _ = context;

        var envelope = CreateTestEnvelope();
        await inbox.RecordAsync(envelope, TestConsumerType, CancellationToken.None);

        // Act 1 — clock has not advanced, message should be retained
        var deletedBefore = await inbox.CleanupAsync(TimeSpan.FromDays(7), CancellationToken.None);
        Assert.Equal(0, deletedBefore);

        // Advance the fake clock past the retention period
        fakeTime.Advance(TimeSpan.FromDays(8));

        // Act 2 — now the message is older than the retention period
        var deletedAfter = await inbox.CleanupAsync(TimeSpan.FromDays(7), CancellationToken.None);

        // Assert
        Assert.Equal(1, deletedAfter);
    }

    private async Task<(TestDbContext Context, PostgresMessageInbox Inbox)> CreateInboxAsync(
        TimeProvider? timeProvider = null)
    {
        var connectionString = await _fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        var context = new TestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(CancellationToken.None);
        }

        var queries = PostgresMessageInboxQueries.From(new InboxTableInfo());
        var inbox = new PostgresMessageInbox(context, connection, queries, timeProvider ?? TimeProvider.System);
        return (context, inbox);
    }

    private static MessageEnvelope CreateTestEnvelope()
    {
        return new MessageEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "urn:message:test-event",
            DestinationAddress = "memory://test/queue",
            SentAt = DateTimeOffset.UtcNow,
            Body = "{\"value\":42}"u8.ToArray()
        };
    }
}
