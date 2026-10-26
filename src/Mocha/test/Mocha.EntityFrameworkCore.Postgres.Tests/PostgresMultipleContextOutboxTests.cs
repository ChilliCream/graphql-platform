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
    public async Task Set_Should_WakeEveryOutboxProcessor_When_MultipleContextsAreIdle()
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

        await using var provider = await CreateProviderAsync(firstConnection, secondConnection, false);
        var firstIdle = ObserveFirstPollCompletion<FirstDbContext>(provider);
        var secondIdle = ObserveFirstPollCompletion<SecondDbContext>(provider);
        var signal = provider.GetRequiredService<IOutboxSignal>();
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
            var recorder = provider.GetRequiredService<MessageRecorder>();
            await recorder.Completed.Task.WaitAsync(TimeSpan.FromSeconds(30), TestToken);

            // assert
            Assert.Equal(["first", "second"], recorder.Messages.Order().ToArray());
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
    public async Task OutboxPublishAsync_Should_SaveAndDeliverIndependently_When_ContextsUseDifferentDatabases()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(firstConnection, secondConnection, false);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var first = scope.ServiceProvider.GetRequiredService<FirstDbContext>();
        var second = scope.ServiceProvider.GetRequiredService<SecondDbContext>();
        var counts = new List<(int First, int Second)>();

        // act
        await first.Outbox(bus).PublishAsync(new ContextEvent("first"), TestToken);
        await second.Outbox(bus).PublishAsync(new ContextEvent("second"), TestToken);
        counts.Add(await CountRowsAsync(provider));
        await first.SaveChangesAsync(TestToken);
        counts.Add(await CountRowsAsync(provider));
        await second.SaveChangesAsync(TestToken);
        counts.Add(await CountRowsAsync(provider));

        var workers = provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().ToArray();
        foreach (var worker in workers)
        {
            await worker.StartAsync(TestToken);
        }

        try
        {
            var recorder = provider.GetRequiredService<MessageRecorder>();
            await recorder.Completed.Task.WaitAsync(TimeSpan.FromSeconds(30), TestToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            while (await CountRowsAsync(provider) != (0, 0))
            {
                await Task.Delay(25, timeout.Token);
            }

            counts.Add(await CountRowsAsync(provider));

            // assert
            Assert.Equal([(0, 0), (1, 0), (1, 1), (0, 0)], counts);
            Assert.Equal(["first", "second"], recorder.Messages.Order().ToArray());
            Assert.Equal([(true, false), (true, false)], recorder.Transactions);
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
    public async Task PublishAsync_Should_PersistToSelectedDefault_When_MultipleContextsAreRegistered()
    {
        // arrange
        var firstConnection = await fixture.CreateDatabaseAsync();
        var secondConnection = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(firstConnection, secondConnection, true);
        await using var scope = provider.CreateAsyncScope();
        var second = scope.ServiceProvider.GetRequiredService<SecondDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var transaction = await second.Database.BeginTransactionAsync(TestToken);

        // act
        await bus.PublishAsync(new ContextEvent("default"), TestToken);
        var beforeCommit = await CountRowsAsync(provider);
        await transaction.CommitAsync(TestToken);
        var afterCommit = await CountRowsAsync(provider);

        // assert
        Assert.Equal(((0, 0), (0, 1)), (beforeCommit, afterCommit));
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

    private static async Task<ServiceProvider> CreateProviderAsync(
        string firstConnection,
        string secondConnection,
        bool selectDefault)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        services.AddDbContextFactory<FirstDbContext>(options => options.UseTestNpgsql(firstConnection));
        services.AddDbContext<SecondDbContext>(options => options.UseTestNpgsql(secondConnection));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<FirstDbContext>(ef => ef.UsePostgresOutbox().UseTransaction());
        builder.AddEntityFramework<SecondDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<ContextEventHandler>();
        builder.AddInMemory();
        if (selectDefault)
        {
            builder.UseDefaultOutbox<SecondDbContext>();
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FirstDbContext>().Database.EnsureCreatedAsync(TestToken);
        await scope.ServiceProvider.GetRequiredService<SecondDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed record ContextEvent(string Context);

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
