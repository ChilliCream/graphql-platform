using System.Collections.Concurrent;
using System.Text.Json;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxAmbientTransactionSignalTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PersistAsync_Should_SignalOnlyAfterCommit_When_AmbientTransactionIsUsed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new VisibilityProbeSignal(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, signal, addHandler: false);
        await EnsureCreatedAsync(provider);
        signal.Events.Clear();

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new TestEvent { Payload = "ambient" }, TestToken);
            signal.Mark("before-complete");
            transaction.Complete();
        }

        // assert
        AssertSignaledOnlyAfterCommit(signal);
    }

    [Fact]
    public async Task SaveChanges_Should_SignalOnlyAfterCommit_When_AmbientTransactionIsUsed()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new VisibilityProbeSignal(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, signal, addHandler: false);
        await EnsureCreatedAsync(provider);
        signal.Events.Clear();

        // act
        using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            db.Add(new OutboxMessage(Guid.NewGuid(), JsonDocument.Parse("{}")));
            await db.SaveChangesAsync(TestToken);
            signal.Mark("before-complete");
            transaction.Complete();
        }

        // assert
        AssertSignaledOnlyAfterCommit(signal);
    }

    [Fact]
    public async Task PersistAsync_Should_SignalOnCompletion_When_AmbientTransactionRollsBack()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new VisibilityProbeSignal(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, signal, addHandler: false);
        await EnsureCreatedAsync(provider);
        signal.Events.Clear();

        // act
        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            await using var scope = provider.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new TestEvent { Payload = "ambient" }, TestToken);
            signal.Mark("before-dispose");
        }

        signal.Mark("after-rollback");

        // assert
        Assert.Equal(
            ["before-dispose", "set(visible=0)", "after-rollback"],
            signal.Events.Distinct().ToArray());
    }

    [Fact]
    public async Task Worker_Should_DeliverMessage_When_PublishedInsideAmbientTransaction()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new MessageRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ResilientOutboxSignal(),
            addHandler: true,
            recorder);
        await EnsureCreatedAsync(provider);
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(TestToken);
        }

        try
        {
            // worker drained the empty table and is parked on the signal
            await PublishAsync(provider, "warm-up");
            Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(10)));

            // act
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await PublishAsync(provider, "ambient");

                // give the prematurely woken worker time to find nothing and park again
                await Task.Delay(TimeSpan.FromSeconds(1), TestToken);
                transaction.Complete();
            }

            var delivered = await recorder.WaitAsync(TimeSpan.FromSeconds(10));

            // assert
            Assert.True(delivered, "message committed inside a TransactionScope was not delivered");
        }
        finally
        {
            foreach (var service in hostedServices)
            {
                await service.StopAsync(CancellationToken.None);
            }

            await Task.Delay(250, CancellationToken.None);
        }
    }

    private static void AssertSignaledOnlyAfterCommit(VisibilityProbeSignal signal)
    {
        var events = signal.Events.ToArray();
        var setsBeforeComplete = events.TakeWhile(e => e != "before-complete").Count(e => e.StartsWith("set"));
        var setsSeeingNoRows = events.Count(e => e == "set(visible=0)");
        var setsAfterComplete = events.SkipWhile(e => e != "before-complete").Count(e => e.StartsWith("set"));
        Assert.Equal((0, 0), (setsBeforeComplete, setsSeeingNoRows));
        Assert.NotEqual(0, setsAfterComplete);
    }

    private static async Task PublishAsync(IServiceProvider provider, string payload)
    {
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new TestEvent { Payload = payload }, TestToken);
    }

    private static async Task EnsureCreatedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await db.Database.EnsureCreatedAsync(TestToken);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        IOutboxSignal signal,
        bool addHandler,
        MessageRecorder? recorder = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder ?? new MessageRecorder());
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        services.AddSingleton(signal);

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        if (addHandler)
        {
            builder.AddEventHandler<TestEventHandler>();
        }

        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);
        return provider;
    }

    public sealed class TestEvent
    {
        public required string Payload { get; init; }
    }

    public sealed class TestEventHandler(MessageRecorder recorder) : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public void Record(object message) => _semaphore.Release();

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphore.WaitAsync(timeout);
    }

    private sealed class VisibilityProbeSignal(string connectionString) : IOutboxSignal
    {
        private readonly string _connectionString =
            new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false }.ConnectionString;

        public ConcurrentQueue<string> Events { get; } = new();

        public void Mark(string label) => Events.Enqueue(label);

        public void Set()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
            Events.Enqueue($"set(visible={command.ExecuteScalar()})");
        }

        public Task WaitAsync(CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
