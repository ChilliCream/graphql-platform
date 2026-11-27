using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresMultipleContextOutboxTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Set_Should_WakeOnlyTheProcessorOfItsContext_When_MultipleContextsAreIdle()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();

        // Initialize the schemas without outbox interceptors.
        await using (var first = new FirstDbContext(new DbContextOptionsBuilder<FirstDbContext>()
            .UseTestNpgsql(firstConnection).Options))
        {
            await first.Database.EnsureCreatedAsync(TestToken);
        }

        await using (var second = new SecondDbContext(new DbContextOptionsBuilder<SecondDbContext>()
            .UseTestNpgsql(secondConnection).Options))
        {
            await second.Database.EnsureCreatedAsync(TestToken);
        }

        await using var provider = await CreateProviderAsync(firstConnection, secondConnection, wakeOnlyOnSignal: true);
        var firstIdle = ObserveFirstPollCompletion<FirstDbContext>(provider);
        var secondIdle = ObserveFirstPollCompletion<SecondDbContext>(provider);
        var signal = provider.GetRequiredKeyedService<IOutboxSignal>(typeof(SecondDbContext));
        var recorder = provider.GetRequiredService<MessageRecorder>();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(ContextEvent));
        var endpoint = runtime.GetPublishEndpoint(type);
        var workers = provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().ToArray();
        foreach (var worker in workers)
        {
            await worker.StartAsync(TestToken);
        }

        try
        {
            await Task.WhenAll(firstIdle, secondIdle).WaitAsync(TimeSpan.FromSeconds(30), TestToken);
            foreach (var (connection, context) in new[] { (firstConnection, "first"), (secondConnection, "second") })
            {
                await MessageRows.InsertOutboxAsync(connection, new MessageEnvelope
                {
                    MessageId = Guid.NewGuid().ToString(),
                    MessageType = type.Identity,
                    ContentType = MessageContentType.Json.Value,
                    DestinationAddress = endpoint.Address.ToString(),
                    Body = JsonSerializer.SerializeToUtf8Bytes(new ContextEvent(context))
                }, TestToken);
            }

            // act
            signal.Set();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            while (recorder.Messages.IsEmpty)
            {
                await Task.Delay(25, timeout.Token);
            }

            // assert
            Assert.Equal(["second"], recorder.Messages.ToArray());
            Assert.Equal(1, await MessageRows.CountOutboxAsync(firstConnection, TestToken));
        }
        finally
        {
            foreach (var worker in workers)
            {
                await worker.StopAsync(TestToken);
            }
        }
    }

#if NET9_0_OR_GREATER
    [Fact]
    public async Task CommitAsync_Should_SignalOnlyItsContext_When_MultipleContextsAreRegistered()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();
        var firstSignal = new StubOutboxSignal();
        var secondSignal = new StubOutboxSignal();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IOutboxSignal>(typeof(FirstDbContext), firstSignal);
        services.AddKeyedSingleton<IOutboxSignal>(typeof(SecondDbContext), secondSignal);
        await using var provider = await CreateProviderAsync(
            services,
            firstConnection,
            secondConnection,
            wakeOnlyOnSignal: false);
        await using var scope = provider.CreateAsyncScope();
        var second = scope.ServiceProvider.GetRequiredService<SecondDbContext>();
        using var outbox = PostgresMessageOutbox.Create(
            typeof(SecondDbContext),
            typeof(SecondDbContext).FullName!,
            scope.ServiceProvider);
        await using var transaction = await second.Database.BeginTransactionAsync(TestToken);
        await outbox.PersistAsync(
            new MessageEnvelope
            {
                MessageId = Guid.NewGuid().ToString(),
                MessageType = "urn:message:context-event",
                DestinationAddress = "memory://test/queue",
                Body = "{\"context\":\"second\"}"u8.ToArray()
            },
            TestToken);

        // act
        await transaction.CommitAsync(TestToken);

        // assert
        Assert.Equal((0, 1), (firstSignal.SetCallCount, secondSignal.SetCallCount));
    }
#endif

    [Fact]
    public async Task PublishAsync_Should_PersistToFirstRegisteredContext_When_MultipleContextsAreRegistered()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            firstConnection,
            secondConnection,
            wakeOnlyOnSignal: false);
        await using var scope = provider.CreateAsyncScope();
        var first = scope.ServiceProvider.GetRequiredService<FirstDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var transaction = await first.Database.BeginTransactionAsync(TestToken);

        // act
        await bus.PublishAsync(new ContextEvent("default"), TestToken);
        var beforeCommit = await CountRowsAsync(provider);
        await transaction.CommitAsync(TestToken);
        var afterCommit = await CountRowsAsync(provider);

        var workers = provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().ToArray();
        foreach (var worker in workers)
        {
            await worker.StartAsync(TestToken);
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            while (recorder.Messages.IsEmpty)
            {
                await Task.Delay(25, timeout.Token);
            }

            // assert
            Assert.Equal((0, 0), beforeCommit);
            Assert.Equal((1, 0), afterCommit);
            Assert.Equal(["default"], recorder.Messages.ToArray());
        }
        finally
        {
            foreach (var worker in workers)
            {
                await worker.StopAsync(TestToken);
            }
        }
    }

    [Fact]
    public async Task RequestAsync_Should_ReturnResponse_When_MultipleContextsAreRegistered()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            firstConnection,
            secondConnection,
            wakeOnlyOnSignal: false);
        var workers = provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().ToArray();
        foreach (var worker in workers)
        {
            await worker.StartAsync(TestToken);
        }

        try
        {
            await using var scope = provider.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // act
            var response = await bus.RequestAsync(new ContextRequest("request"), TestToken)
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(30), TestToken);

            // assert
            Assert.Equal("request", response.Context);
        }
        finally
        {
            foreach (var worker in workers)
            {
                await worker.StopAsync(TestToken);
            }
        }
    }

    private static Task ObserveFirstPollCompletion<TContext>(ServiceProvider provider)
        where TContext : DbContext
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TContext).FullName);
        var createConnection = options.CreateConnection;
        options.CreateConnection = services =>
        {
            var connection = createConnection(services);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Closed)
                {
                    completed.TrySetResult();
                }
            };
            return connection;
        };
        return completed.Task;
    }

    private static async Task<(int First, int Second)> CountRowsAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var first = await scope.ServiceProvider.GetRequiredService<FirstDbContext>()
            .Set<OutboxMessage>().CountAsync(TestToken);
        var second = await scope.ServiceProvider.GetRequiredService<SecondDbContext>()
            .Set<OutboxMessage>().CountAsync(TestToken);
        return (first, second);
    }

    // With wakeOnlyOnSignal the relays poll every 10 minutes and no consumer transaction commits, so only the
    // outbox signal wakes an idle relay.
    private static Task<ServiceProvider> CreateProviderAsync(
        string firstConnection,
        string secondConnection,
        bool wakeOnlyOnSignal)
        => CreateProviderAsync(new ServiceCollection(), firstConnection, secondConnection, wakeOnlyOnSignal);

    private static async Task<ServiceProvider> CreateProviderAsync(
        ServiceCollection services,
        string firstConnection,
        string secondConnection,
        bool wakeOnlyOnSignal)
    {
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        services.AddDbContextFactory<FirstDbContext>(options => options.UseTestNpgsql(firstConnection));
        services.AddDbContext<SecondDbContext>(options => options.UseTestNpgsql(secondConnection));
        var builder = services.AddMessageBus();
        Action<PostgresOutboxOptions> configureOutbox = wakeOnlyOnSignal
            ? static options => options.PollingInterval = TimeSpan.FromMinutes(10)
            : static _ => { };
        builder.AddEntityFramework<FirstDbContext>(ef =>
        {
            ef.UsePostgresOutbox(configureOutbox);
            if (!wakeOnlyOnSignal)
            {
                ef.UseTransaction();
            }
        });
        builder.AddEntityFramework<SecondDbContext>(ef => ef.UsePostgresOutbox(configureOutbox));
        builder.AddEventHandler<ContextEventHandler>();
        builder.AddRequestHandler<ContextRequestHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FirstDbContext>().Database.EnsureCreatedAsync(TestToken);
        await scope.ServiceProvider.GetRequiredService<SecondDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed record ContextEvent(string Context);

    public sealed record ContextRequest(string Context) : IEventRequest<ContextResponse>;

    public sealed record ContextResponse(string Context);

    public sealed class ContextRequestHandler : IEventRequestHandler<ContextRequest, ContextResponse>
    {
        public ValueTask<ContextResponse> HandleAsync(ContextRequest request, CancellationToken cancellationToken)
            => new(new ContextResponse(request.Context));
    }

    public sealed class ContextEventHandler(
        MessageRecorder recorder,
        FirstDbContext first,
        SecondDbContext second) : IEventHandler<ContextEvent>
    {
        public ValueTask HandleAsync(ContextEvent message, CancellationToken cancellationToken)
        {
            recorder.Transactions.Enqueue((
                first.Database.CurrentTransaction is not null,
                second.Database.CurrentTransaction is not null));
            recorder.Messages.Enqueue(message.Context);
            if (recorder.Messages.Count == 2)
            {
                recorder.Completed.TrySetResult();
            }

            return default;
        }
    }

    public sealed class MessageRecorder
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ConcurrentQueue<(bool First, bool Second)> Transactions { get; } = new();

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class FirstDbContext(DbContextOptions<FirstDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddPostgresOutbox();
    }

    public sealed class SecondDbContext(DbContextOptions<SecondDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddPostgresOutbox();
    }
}
