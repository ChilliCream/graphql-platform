using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxPollingTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_idlePollingInterval = TimeSpan.FromMilliseconds(200);

    // Longer than the pause of a worker that found a due message held, and far shorter than the message is overdue.
    private static readonly TimeSpan s_takeOverTimeout = TimeSpan.FromSeconds(5);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_DispatchAgainAfterOneAndTwoSeconds_When_AttemptsKeepFailing()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var watch = new Stopwatch();
        var attempts = new ConcurrentQueue<TimeSpan>();
        var dispatch = new ScriptedDispatch(attempt =>
        {
            attempts.Enqueue(watch.Elapsed);

            return attempt.Number <= 2
                ? throw new InvalidOperationException("The broker refused the message.")
                : attempt.SendAsync();
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch, NewSignal(), static _ => { });
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        long remaining;

        // act
        watch.Start();
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        // A message is due one second after its first counted attempt and two seconds after its second one.
        TimeSpan[] times = [.. attempts];
        Assert.Equal(0, remaining);
        Assert.Equal(3, times.Length);
        Assert.InRange(times[1] - times[0], TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2.5));
        Assert.InRange(times[2] - times[1], TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_ExhaustedMessageIsReArmedWithoutSignal()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var refusing = true;
        var dispatch = new ScriptedDispatch(attempt => Volatile.Read(ref refusing)
            ? throw new InvalidOperationException("The broker refused the message.")
            : attempt.SendAsync());
        var firstPassEnded = NewSignal();
        await using var provider = await CreateProviderAsync(
            connectionString, dispatch, firstPassEnded, static o => o.MaxAttempts = 1);
        provider.GetRequiredService<PostgresOutboxProcessor>().IdlePollingInterval = s_idlePollingInterval;
        var id = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        int? timesSentWhenIdle;
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            // The first pass used up the only attempt, and nothing signals the processor after it.
            await firstPassEnded.Task.WaitAsync(s_timeout, TestToken);
            timesSentWhenIdle = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);
            Volatile.Write(ref refusing, false);
            await MessageRows.SetOutboxTimesSentAsync(connectionString, id, 0, TestToken);
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(1, timesSentWhenIdle);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_AnotherProcessInsertsItWithoutSignal()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        var firstPassEnded = NewSignal();
        await using var provider = await CreateProviderAsync(
            connectionString, dispatch, firstPassEnded, static _ => { });
        provider.GetRequiredService<PostgresOutboxProcessor>().IdlePollingInterval = s_idlePollingInterval;
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            // The first pass found the outbox empty, and nothing signals the processor after it.
            await firstPassEnded.Task.WaitAsync(s_timeout, TestToken);
            await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
    }

    [Fact]
    public async Task ProcessAsync_Should_TakeOverOverdueMessage_When_TheProcessorThatHeldItStops()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var held = NewSignal();
        var holderDispatch = new ScriptedDispatch(async attempt =>
        {
            held.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, attempt.CancellationToken);
        });
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        var firstPassEnded = NewSignal();
        await using var holder = await CreateProviderAsync(
            connectionString, holderDispatch, NewSignal(), static o => o.DispatchTimeout = Timeout.InfiniteTimeSpan);
        await using var provider = await CreateProviderAsync(
            connectionString, dispatch, firstPassEnded, static _ => { });
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await MessageRows.MakeOutboxDueAsync(connectionString, TimeSpan.FromHours(1), TestToken);
        var holderProcessor = StartProcessor(holder);
        await held.Task.WaitAsync(s_timeout, TestToken);
        int attemptsWhileHeld;
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            // The first pass found the message held by the other processor and ended without a dispatch.
            await firstPassEnded.Task.WaitAsync(s_timeout, TestToken);
            attemptsWhileHeld = dispatch.AttemptsOf("A");
            await holderProcessor.DisposeAsync();
            remaining = await MessageRows.WaitForOutboxCountAsync(
                connectionString, 0, s_takeOverTimeout, TestToken);
        }

        // assert
        Assert.Equal(0, attemptsWhileHeld);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Empty(holderDispatch.Accepted);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(PollingEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new PollingEvent(messageId))
        };
    }

    /// <summary>
    /// Creates a provider whose outbox processor is never signalled, and completes
    /// <paramref name="firstPassEnded"/> when a worker has read the outbox for the first time.
    /// </summary>
    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        ScriptedDispatch dispatch,
        TaskCompletionSource firstPassEnded,
        Action<PostgresOutboxOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxSignal>(new StubOutboxSignal());
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox(configure));
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("polling");
            t.DispatchEndpoint("polling").ToQueue("polling").Publish<PollingEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatch.Middleware, after: "Serialization"));

        // A worker closes its connection at the end of a pass, before it waits.
        services.PostConfigure<PostgresMessageOutboxOptions>(typeof(TestDbContext).FullName, options =>
        {
            var createConnection = options.CreateConnection;
            options.CreateConnection = sp =>
            {
                var connection = createConnection(sp);
                connection.StateChange += (_, args) =>
                {
                    if (args.CurrentState == ConnectionState.Closed)
                    {
                        firstPassEnded.TrySetResult();
                    }
                };
                return connection;
            };
        });

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

    public sealed record PollingEvent(string Name);

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
