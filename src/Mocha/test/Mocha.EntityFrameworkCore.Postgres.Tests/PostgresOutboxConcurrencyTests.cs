using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxConcurrencyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task ProcessAsync_Should_BoundConcurrentDispatches_When_MessagesArePending(int? concurrency)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var probe = new DispatchProbe(concurrency ?? 1);
        await using var provider = await CreateProviderAsync(connectionString, concurrency, probe);
        await InsertMessagesAsync(provider, connectionString, 0, 12);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processor = provider.GetRequiredService<PostgresOutboxProcessor>();

        // act
        var processing = processor.ProcessAsync(cancellation.Token);
        long pending;
        try
        {
            await probe.Started.Task.WaitAsync(s_timeout, TestToken);
            pending = await CountMessagesAsync(connectionString);
            probe.Release.TrySetResult();
            await WaitForEmptyOutboxAsync(connectionString);
        }
        finally
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
        }

        // assert
        Assert.Equal(12, pending);
        Assert.Equal(concurrency ?? 1, probe.PeakConcurrency);
        Assert.Equal(Enumerable.Range(0, 12), probe.Completed.Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_Should_ResumeIdleWorkers_When_NewMessagesArriveDuringBlockedDispatch(bool signal)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var idleWorkers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closedConnections = 0;
        var probe = new DispatchProbe(4)
        {
            HoldFirst = true,
            ConnectionCreated = connection => connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Closed
                    && Interlocked.Increment(ref closedConnections) == 3)
                {
                    idleWorkers.TrySetResult();
                }
            }
        };
        await using var provider = await CreateProviderAsync(connectionString, 4, probe);
        await InsertMessagesAsync(provider, connectionString, 0, 1);
        if (signal)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(TestToken);
            await using var command = connection.CreateCommand();
            // The blocked row must not schedule a poll within the test's timeout.
            command.CommandText = "UPDATE outbox_messages SET created_at = NOW() - INTERVAL '1 hour'";
            await command.ExecuteNonQueryAsync(TestToken);
        }

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processor = provider.GetRequiredService<PostgresOutboxProcessor>();
        var processing = processor.ProcessAsync(cancellation.Token);
        int[] completedWhileBlocked;

        // act
        try
        {
            await probe.FirstStarted.Task.WaitAsync(s_timeout, TestToken);
            await idleWorkers.Task.WaitAsync(s_timeout, TestToken);
            await InsertMessagesAsync(provider, connectionString, 1, 300);
            if (signal)
            {
                provider.GetRequiredService<IOutboxSignal>().Set();
            }

            await probe.Started.Task.WaitAsync(s_timeout, TestToken);
            probe.Release.TrySetResult();
            await WaitForOutboxCountAsync(connectionString, 1);

            completedWhileBlocked = probe.Completed.Order().ToArray();
            probe.FirstRelease.TrySetResult();
            await WaitForEmptyOutboxAsync(connectionString);
        }
        finally
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
        }

        // assert
        Assert.Equal(Enumerable.Range(1, 300), completedWhileBlocked);
        Assert.Equal(4, probe.PeakConcurrency);
        Assert.Equal(Enumerable.Range(0, 301), probe.Completed.Order());
    }

    [Fact]
    public async Task ProcessAsync_Should_DrainBacklog_When_OneDispatchRemainsBlocked()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var probe = new DispatchProbe(4) { HoldFirst = true };
        await using var provider = await CreateProviderAsync(connectionString, 4, probe);
        await InsertMessagesAsync(provider, connectionString, 0, 501);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processor = provider.GetRequiredService<PostgresOutboxProcessor>();

        // act
        var processing = processor.ProcessAsync(cancellation.Token);
        int[] completedWhileBlocked;
        try
        {
            await probe.FirstStarted.Task.WaitAsync(s_timeout, TestToken);
            await probe.Started.Task.WaitAsync(s_timeout, TestToken);
            probe.Release.TrySetResult();
            await WaitForOutboxCountAsync(connectionString, 1);

            completedWhileBlocked = probe.Completed.Order().ToArray();
        }
        finally
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
        }

        // assert
        Assert.Equal(Enumerable.Range(1, 500), completedWhileBlocked);
        Assert.Equal(4, probe.PeakConcurrency);
    }

    [Fact]
    public async Task ProcessAsync_Should_ReleaseClaims_When_ConcurrentDispatchesAreCancelled()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var probe = new DispatchProbe(4);
        await using var provider = await CreateProviderAsync(connectionString, 4, probe);
        await InsertMessagesAsync(provider, connectionString, 0, 12);
        var processor = provider.GetRequiredService<PostgresOutboxProcessor>();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = processor.ProcessAsync(cancellation.Token);
        await probe.Started.Task.WaitAsync(s_timeout, TestToken);

        // act
        await cancellation.CancelAsync();
        await processing.WaitAsync(s_timeout, TestToken);
        var pendingAfterCancellation = await CountMessagesAsync(connectionString);
        probe.Release.TrySetResult();
        using var restartCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var restarted = processor.ProcessAsync(restartCancellation.Token);
        try
        {
            await WaitForEmptyOutboxAsync(connectionString);
        }
        finally
        {
            await restartCancellation.CancelAsync();
            await restarted.WaitAsync(s_timeout, TestToken);
        }

        // assert
        Assert.Equal(12, pendingAfterCancellation);
        Assert.Equal(0, probe.ActiveDispatches);
        Assert.Equal(Enumerable.Range(0, 12), probe.Completed.Order());
    }

    [Fact]
    public async Task ProcessAsync_Should_KeepDispatchingOtherMessages_When_OneDispatchFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var probe = new DispatchProbe(4) { FailFirst = true };
        await using var provider = await CreateProviderAsync(connectionString, 4, probe);
        await InsertMessagesAsync(provider, connectionString, 0, 12);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processor = provider.GetRequiredService<PostgresOutboxProcessor>();

        // act
        var processing = processor.ProcessAsync(cancellation.Token);
        int[] completedWhileFailing;
        bool stoppedByFailure;
        try
        {
            await probe.Started.Task.WaitAsync(s_timeout, TestToken);
            probe.Release.TrySetResult();
            await WaitForOutboxCountAsync(connectionString, 1);
            completedWhileFailing = probe.Completed.Order().ToArray();
            stoppedByFailure = processing.IsCompleted;
        }
        finally
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
        }

        // assert
        Assert.Equal(Enumerable.Range(1, 11), completedWhileFailing);
        Assert.False(stoppedByFailure);
        Assert.Equal(0, probe.ActiveDispatches);
    }

    [Fact]
    public async Task Outbox_Should_DeliverMessageOnce_When_AnotherDispatchFailsBeforeItsDispatchReturns()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var acceptedA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var returnA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt is { MessageId: "B", Number: 1 })
            {
                // B fails only after the transport accepted A.
                await acceptedA.Task.WaitAsync(attempt.CancellationToken);
                throw new InvalidOperationException("The dispatch of B failed.");
            }

            await attempt.SendAsync();

            if (attempt is { MessageId: "A", Number: 1 })
            {
                // The transport has A, and the dispatch of A returns only after B has failed.
                acceptedA.TrySetResult();
                await attempt.WaitForCancellationAsync(returnA.Task);
            }
        });
        await using var provider = await CreateProviderAsync(connectionString, 2, dispatch.Middleware, null);
        var rowOfA = await InsertMessageAsync(provider, connectionString, "A", 0);
        var rowOfB = await InsertMessageAsync(provider, connectionString, "B", 1);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);

        // act
        var timesSentOfB = await MessageRows.WaitForOutboxTimesSentAsync(
            connectionString, rowOfB, 1, s_timeout, TestToken);
        returnA.TrySetResult();
        var removedA = await MessageRows.WaitForOutboxRemovalAsync(connectionString, rowOfA, s_timeout, TestToken);
        await MessageRows.MakeOutboxDueAsync(connectionString, TimeSpan.FromHours(1), TestToken);
        provider.GetRequiredService<IOutboxSignal>().Set();
        await WaitForEmptyOutboxAsync(connectionString);

        // assert
        Assert.Equal(1, timesSentOfB);
        Assert.True(removedA);
        Assert.Equal(["A", "B"], dispatch.Accepted.Order());
    }

    private static Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        int? concurrency,
        DispatchProbe probe)
        => CreateProviderAsync(
            connectionString,
            concurrency,
            new DispatchMiddlewareConfiguration(
                (_, next) => context => probe.DispatchAsync(context, next),
                "DispatchProbe"),
            probe.ConnectionCreated);

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        int? concurrency,
        DispatchMiddlewareConfiguration dispatchMiddleware,
        Action<NpgsqlConnection>? connectionCreated)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox(o =>
        {
            if (concurrency is { } value)
            {
                o.MaxConcurrentMessages = value;
            }
        }));
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("pending");
            t.DispatchEndpoint("pending").ToQueue("pending").Publish<PendingEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatchMiddleware, after: "Serialization"));

        if (connectionCreated is { } onConnectionCreated)
        {
            services.PostConfigure<PostgresMessageOutboxOptions>(typeof(TestDbContext).FullName, options =>
            {
                var createConnection = options.CreateConnection;
                options.CreateConnection = sp =>
                {
                    var connection = createConnection(sp);
                    onConnectionCreated(connection);
                    return connection;
                };
            });
        }

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private static async Task InsertMessagesAsync(
        IServiceProvider provider,
        string connectionString,
        int startIndex,
        int count)
    {
        for (var i = startIndex; i < startIndex + count; i++)
        {
            await InsertMessageAsync(provider, connectionString, i.ToString(), i);
        }
    }

    private static async Task<Guid> InsertMessageAsync(
        IServiceProvider provider,
        string connectionString,
        string messageId,
        int index)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(PendingEvent));
        return await MessageRows.InsertOutboxAsync(
            connectionString,
            new MessageEnvelope
            {
                MessageId = messageId,
                MessageType = type.Identity,
                ContentType = MessageContentType.Json.Value,
                DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
                Body = JsonSerializer.SerializeToUtf8Bytes(new PendingEvent(index))
            },
            TestToken);
    }

    private static async Task<long> CountMessagesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static Task WaitForEmptyOutboxAsync(string connectionString)
        => WaitForOutboxCountAsync(connectionString, 0);

    private static async Task WaitForOutboxCountAsync(string connectionString, long count)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        timeout.CancelAfter(s_timeout);
        while (await CountMessagesAsync(connectionString) != count)
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    public sealed record PendingEvent(int Index);

    private sealed class DispatchProbe(int concurrency)
    {
        private int _active;
        private int _peak;

        public bool FailFirst { get; set; }
        public bool HoldFirst { get; set; }
        public Action<NpgsqlConnection>? ConnectionCreated { get; set; }
        public int ActiveDispatches => Volatile.Read(ref _active);
        public int PeakConcurrency => Volatile.Read(ref _peak);
        public ConcurrentQueue<int> Completed { get; } = new();
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask DispatchAsync(IDispatchContext context, DispatchDelegate next)
        {
            var active = Interlocked.Increment(ref _active);
            int previous;
            do
            {
                previous = Volatile.Read(ref _peak);
            }
            while (active > previous && Interlocked.CompareExchange(ref _peak, active, previous) != previous);

            if (active >= concurrency)
            {
                Started.TrySetResult();
            }

            try
            {
                var index = int.Parse(context.Envelope!.MessageId!);
                if (HoldFirst && index == 0)
                {
                    FirstStarted.TrySetResult();
                    await FirstRelease.Task.WaitAsync(context.CancellationToken);
                }

                if (FailFirst && index == 0)
                {
                    await Started.Task.WaitAsync(context.CancellationToken);
                    throw new InvalidOperationException("The first dispatch failed.");
                }

                await Release.Task.WaitAsync(context.CancellationToken);
                await next(context);
                Completed.Enqueue(index);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }
}
