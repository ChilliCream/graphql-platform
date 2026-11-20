using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxRetryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_AttemptCountExceedsFormerLimit()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.Number == 1
            ? throw new InvalidOperationException("The broker is unreachable.")
            : attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        var id = await MessageRows.InsertOutboxAsync(
            connectionString, CreateEnvelope(provider, "A"), timesSent: 10, TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            await MessageRows.WaitForOutboxTimesSentAsync(connectionString, id, 11, s_timeout, TestToken);
            await MessageRows.MakeOutboxDueAsync(connectionString, TestToken);
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Equal(2, dispatch.AttemptsOf("A"));
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessageAgain_When_RetryDelayElapsed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.Number == 1
            ? throw new InvalidOperationException("The broker is unreachable.")
            : attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
    }

    [Fact]
    public async Task ProcessAsync_Should_ScheduleRetryTenSecondsAhead_When_DispatchFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(
            static _ => throw new InvalidOperationException("The broker is unreachable."));
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        var id = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        int? timesSent;

        // act
        await using (StartProcessor(provider))
        {
            timesSent = await MessageRows.WaitForOutboxTimesSentAsync(connectionString, id, 1, s_timeout, TestToken);
        }

        var secondsUntilDue = await MessageRows.GetOutboxSecondsUntilDueAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(1, timesSent);
        Assert.InRange(secondsUntilDue, 5, 10);
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_OtherInstanceReleasesDueMessage()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await using var otherInstance = new NpgsqlConnection(connectionString);
        await otherInstance.OpenAsync(TestToken);
        await using var otherTransaction = await otherInstance.BeginTransactionAsync(TestToken);
        await using (var lockRow = otherInstance.CreateCommand())
        {
            lockRow.Transaction = otherTransaction;
            lockRow.CommandText = """SELECT "id" FROM "outbox_messages" FOR UPDATE;""";
            await lockRow.ExecuteNonQueryAsync(TestToken);
        }

        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestToken);
            await otherTransaction.RollbackAsync(TestToken);
            remaining = await MessageRows.WaitForOutboxCountAsync(
                connectionString, 0, TimeSpan.FromSeconds(5), TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Equal(1, dispatch.AttemptsOf("A"));
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchLaterMessage_When_EarlierMessageWaitsForRetry()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.MessageId == "A"
            ? throw new InvalidOperationException("The broker rejected the message.")
            : attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        var earlier = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "B"), TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 1, s_timeout, TestToken);
            await MessageRows.WaitForOutboxTimesSentAsync(connectionString, earlier, 1, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(1, remaining);
        Assert.Equal(["B"], dispatch.Accepted);
        Assert.Equal(1, await MessageRows.GetOutboxTimesSentAsync(connectionString, earlier, TestToken));
    }

    [Fact]
    public async Task NextPollingInterval_Should_ReturnNull_When_OutboxIsEmpty()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await EnsureCreatedAsync(connectionString);

        // act
        var result = await ExecuteNextPollingIntervalAsync(connectionString);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task NextPollingInterval_Should_ReturnSecondsUntilEarliestRow_When_RowsAreNotDue()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await EnsureCreatedAsync(connectionString);
        await InsertRowsAsync(connectionString, "NOW() + INTERVAL '60 seconds'", "NOW() + INTERVAL '30 seconds'");

        // act
        var result = await ExecuteNextPollingIntervalAsync(connectionString);

        // assert
        Assert.InRange(Assert.IsType<double>(result), 25, 30);
    }

    [Fact]
    public async Task NextPollingInterval_Should_ReturnNegativeSeconds_When_RowIsDue()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await EnsureCreatedAsync(connectionString);
        await InsertRowsAsync(connectionString, "NOW() + INTERVAL '60 seconds'", "NOW() - INTERVAL '30 seconds'");

        // act
        var result = await ExecuteNextPollingIntervalAsync(connectionString);

        // assert
        Assert.InRange(Assert.IsType<double>(result), -35, -25);
    }

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(RetryEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new RetryEvent(messageId))
        };
    }

    private async Task EnsureCreatedAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
    }

    private static async Task InsertRowsAsync(string connectionString, params string[] createdAtExpressions)
    {
        const string emptyEnvelope = "'{}'";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);

        foreach (var createdAt in createdAtExpressions)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"""
                 INSERT INTO "outbox_messages" ("id", "envelope", "times_sent", "created_at")
                 VALUES (gen_random_uuid(), {emptyEnvelope}, 0, {createdAt});
                 """;
            await command.ExecuteNonQueryAsync(TestToken);
        }
    }

    private static async Task<object?> ExecuteNextPollingIntervalAsync(string connectionString)
    {
        var queries = PostgresMessageOutboxQueries.From(new OutboxTableInfo());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = queries.NextPollingInterval;

        var result = await command.ExecuteScalarAsync(TestToken);

        return result is DBNull ? null : result;
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        ScriptedDispatch dispatch)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("retry");
            t.DispatchEndpoint("retry").ToQueue("retry").Publish<RetryEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatch.Middleware, after: "Serialization"));

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private static RunningProcessor StartProcessor(IServiceProvider provider)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = provider.GetRequiredService<PostgresOutboxProcessor>().ProcessAsync(cancellation.Token);
        return new RunningProcessor(processing, cancellation);
    }

    public sealed record RetryEvent(string Name);

    // Stops the processor like a host does and waits until it has returned.
    private sealed class RunningProcessor(Task processing, CancellationTokenSource cancellation) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
            cancellation.Dispose();
        }
    }
}
