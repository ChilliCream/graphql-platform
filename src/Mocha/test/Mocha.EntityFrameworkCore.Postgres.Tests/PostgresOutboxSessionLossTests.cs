using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxSessionLossTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessageAgain_When_SessionEndsAfterTransportAcceptedIt()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var endedSessions = 0L;
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            await attempt.SendAsync();

            if (attempt.Number == 1)
            {
                endedSessions = await EndSessionsInTransactionAsync(connectionString);
            }
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(1, endedSessions);
        Assert.Equal(0, remaining);
        Assert.Equal(["A", "A"], dispatch.Accepted);
    }

    [Fact]
    public async Task ProcessAsync_Should_PauseLongerBeforeEachDispatch_When_SessionEndsAfterEveryAccept()
    {
        // arrange
        const int lostSessions = 4;
        var connectionString = await fixture.CreateDatabaseAsync();
        var watch = Stopwatch.StartNew();
        var endedSessions = 0L;
        var returned = TimeSpan.Zero;
        var paused = TimeSpan.Zero;
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt.Number > 1)
            {
                paused += watch.Elapsed - returned;
            }

            await attempt.SendAsync();

            if (attempt.Number <= lostSessions)
            {
                endedSessions += await EndSessionsInTransactionAsync(connectionString);
            }

            returned = watch.Elapsed;
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        // The four pauses double from at least 100 milliseconds, which adds up to at least 1.5 seconds. Pauses
        // that start over after every accepted dispatch add up to less than 0.8 seconds.
        Assert.Equal(lostSessions, endedSessions);
        Assert.Equal(0, remaining);
        Assert.Equal(lostSessions + 1, dispatch.AttemptsOf("A"));
        Assert.InRange(paused, TimeSpan.FromSeconds(1.2), s_timeout);
    }

    /// <summary>
    /// Ends every other session of the database that has a transaction open, as the session of a worker has
    /// while it dispatches, and returns how many sessions ended.
    /// </summary>
    private static async Task<long> EndSessionsInTransactionAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();

        // The second argument makes the function wait until the session has ended.
        command.CommandText =
            """
            SELECT COUNT(*) FILTER (WHERE pg_terminate_backend(pid, 10000))
            FROM pg_stat_activity
            WHERE datname = current_database() AND pid <> pg_backend_pid() AND state = 'idle in transaction';
            """;

        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(SessionLossEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new SessionLossEvent(messageId))
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, ScriptedDispatch dispatch)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("session-loss");
            t.DispatchEndpoint("session-loss").ToQueue("session-loss").Publish<SessionLossEvent>();
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

    public sealed record SessionLossEvent(string Name);

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
