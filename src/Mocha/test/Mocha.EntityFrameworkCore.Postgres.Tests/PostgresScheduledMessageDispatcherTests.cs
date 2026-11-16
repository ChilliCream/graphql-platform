using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresScheduledMessageDispatcherTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private const string EmptyEnvelope = "{}";
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_AttemptCountExceedsFormerLimit()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await ExecuteAsync(connectionString, """UPDATE "scheduled_messages" SET "times_sent" = 10;""");
        object? remaining;

        // act
        await using (StartDispatcher(provider))
        {
            remaining = await WaitForAsync(
                connectionString,
                """SELECT COUNT(*) FROM "scheduled_messages";""",
                static count => count is 0L);
        }

        // assert
        Assert.Equal(0L, remaining);
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
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        object? timesSent;

        // act
        await using (StartDispatcher(provider))
        {
            timesSent = await WaitForAsync(
                connectionString,
                """SELECT "times_sent" FROM "scheduled_messages";""",
                static count => count is 1);
        }

        var secondsUntilDue = await ExecuteScalarAsync(
            connectionString,
            """
            SELECT (EXTRACT(EPOCH FROM "scheduled_time") - EXTRACT(EPOCH FROM NOW()))::double precision
            FROM "scheduled_messages";
            """);

        // assert
        Assert.Equal(1, timesSent);
        Assert.InRange(Assert.IsType<double>(secondsUntilDue), 5, 10);
    }

    [Fact]
    public async Task ProcessAsync_Should_NotDispatchNextMessage_When_DispatchFailed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.MessageId == "A"
            ? throw new InvalidOperationException("The broker is unreachable.")
            : attempt.SendAsync());
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "B"), TestToken);
        object? remaining;

        // act
        await using (StartDispatcher(provider))
        {
            await WaitForAsync(
                connectionString,
                """SELECT MAX("times_sent") FROM "scheduled_messages";""",
                static count => count is 1);
            await Task.Delay(TimeSpan.FromMilliseconds(500), TestToken);
            remaining = await ExecuteScalarAsync(connectionString, """SELECT COUNT(*) FROM "scheduled_messages";""");
        }

        // assert
        Assert.Equal(2L, remaining);
        Assert.Equal(1, dispatch.AttemptsOf("A"));
        Assert.Equal(0, dispatch.AttemptsOf("B"));
    }

    [Fact]
    public async Task NextWakeInterval_Should_ReturnNull_When_NoMessageIsScheduled()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ScriptedDispatch(static attempt => attempt.SendAsync()));

        // act
        var result = await ExecuteScalarAsync(connectionString, NextWakeInterval);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public async Task NextWakeInterval_Should_ReturnSecondsUntilEarliestMessage_When_MessagesAreNotDue()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ScriptedDispatch(static attempt => attempt.SendAsync()));
        await InsertRowAsync(connectionString, "NOW() + INTERVAL '60 seconds'", timesSent: 0);
        await InsertRowAsync(connectionString, "NOW() + INTERVAL '30 seconds'", timesSent: 3);

        // act
        var result = await ExecuteScalarAsync(connectionString, NextWakeInterval);

        // assert
        Assert.InRange(Assert.IsType<double>(result), 25, 30);
    }

    [Fact]
    public async Task NextWakeInterval_Should_ReturnNegativeSeconds_When_MessagePastFormerLimitIsDue()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ScriptedDispatch(static attempt => attempt.SendAsync()));
        await InsertRowAsync(connectionString, "NOW() + INTERVAL '60 seconds'", timesSent: 0);
        await InsertRowAsync(connectionString, "NOW() - INTERVAL '30 seconds'", timesSent: 10);

        // act
        var result = await ExecuteScalarAsync(connectionString, NextWakeInterval);

        // assert
        Assert.InRange(Assert.IsType<double>(result), -35, -25);
    }

    private static string NextWakeInterval
        => ScheduledMessageQueries.From(new ScheduledMessageTableInfo()).NextWakeInterval;

    private static Task InsertRowAsync(string connectionString, string scheduledTime, int timesSent)
        => ExecuteAsync(
            connectionString,
            $"""
             INSERT INTO "scheduled_messages"
                 ("id", "envelope", "scheduled_time", "times_sent", "max_attempts", "created_at")
             VALUES (gen_random_uuid(), '{EmptyEnvelope}', {scheduledTime}, {timesSent}, 10, NOW());
             """);

    private static Task ExecuteAsync(string connectionString, string sql)
        => ExecuteScalarAsync(connectionString, sql);

    private static async Task<object?> ExecuteScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var result = await command.ExecuteScalarAsync(TestToken);

        return result is DBNull ? null : result;
    }

    // Polls the query until its value is the expected one or the timeout elapsed, and returns the last value.
    private static async Task<object?> WaitForAsync(string connectionString, string sql, Func<object?, bool> isExpected)
    {
        var deadline = DateTime.UtcNow + s_timeout;
        var value = await ExecuteScalarAsync(connectionString, sql);

        while (!isExpected(value) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestToken);
            value = await ExecuteScalarAsync(connectionString, sql);
        }

        return value;
    }

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(ScheduledEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new ScheduledEvent(messageId))
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, ScriptedDispatch dispatch)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresScheduling());
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("scheduled");
            t.DispatchEndpoint("scheduled").ToQueue("scheduled").Publish<ScheduledEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatch.Middleware, after: "Serialization"));

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private static RunningDispatcher StartDispatcher(IServiceProvider provider)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = provider.GetRequiredService<ScheduledMessageDispatcher>().ProcessAsync(cancellation.Token);
        return new RunningDispatcher(processing, cancellation);
    }

    public sealed record ScheduledEvent(string Name);

    // Stops the dispatcher like a host does and waits until it has returned.
    private sealed class RunningDispatcher(Task processing, CancellationTokenSource cancellation) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
            cancellation.Dispose();
        }
    }
}
