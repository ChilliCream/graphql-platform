using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxTransactionOrderTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Outbox_Should_DeliverTransactionInPublishOrder_When_FirstMessageFailsOnce()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var listener = new DispatchListener { ThrowOnFirstDispatch = true };
        var provider = await CreateProviderAsync(connectionString, recorder, listener);
        var transactionId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            await InsertAsync(connectionString, provider, transactionId, 0, i, unresolvable: false);
        }

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await recorder.WaitForAsync(5, s_timeout);

        // assert
        Assert.Equal([0, 1, 2, 3, 4], recorder.Events.Select(e => e.Index));
    }

    [Fact]
    public async Task Outbox_Should_KeepLaterMessages_When_MiddleMessageFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var provider = await CreateProviderAsync(connectionString, recorder, new DispatchListener());
        var transactionId = Guid.NewGuid();
        await InsertAsync(connectionString, provider, transactionId, 0, 0, unresolvable: false);
        await InsertAsync(connectionString, provider, transactionId, 0, 1, unresolvable: true);
        await InsertAsync(connectionString, provider, transactionId, 0, 2, unresolvable: false);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var rows = await WaitForRowsAsync(connectionString, r => r.Count == 2 && r[0].TimesSent >= 1);

        // assert
        Assert.Equal([0], recorder.Events.Select(e => e.Index));
        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].TimesSent >= 1);
        Assert.Equal(0, rows[1].TimesSent);
    }

    [Fact]
    public async Task Outbox_Should_DeliverOtherTransactions_When_OneTransactionFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var provider = await CreateProviderAsync(connectionString, recorder, new DispatchListener());
        var failing = Guid.NewGuid();
        var healthy = Guid.NewGuid();
        await InsertAsync(connectionString, provider, failing, 1, 0, unresolvable: true);
        await InsertAsync(connectionString, provider, failing, 1, 1, unresolvable: false);
        await InsertAsync(connectionString, provider, failing, 1, 2, unresolvable: false);
        await InsertAsync(connectionString, provider, healthy, 2, 0, unresolvable: false);
        await InsertAsync(connectionString, provider, healthy, 2, 1, unresolvable: false);
        await InsertAsync(connectionString, provider, healthy, 2, 2, unresolvable: false);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await recorder.WaitForAsync(3, s_timeout);
        var rows = await WaitForRowsAsync(connectionString, r => r.Count == 3 && r[0].TimesSent >= 1);

        // assert
        Assert.Equal([(2, 0), (2, 1), (2, 2)], recorder.Events.Select(e => (e.Transaction, e.Index)));
        Assert.Equal([failing, failing, failing], rows.Select(r => r.TransactionId));
    }

    [Fact]
    public async Task Outbox_Should_DeliverOtherMessages_When_RowWithoutTransactionIdFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var provider = await CreateProviderAsync(connectionString, recorder, new DispatchListener());
        await InsertAsync(connectionString, provider, null, 0, 0, unresolvable: true);
        await InsertAsync(connectionString, provider, null, 0, 1, unresolvable: false);
        await InsertAsync(connectionString, provider, null, 0, 2, unresolvable: false);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await recorder.WaitForAsync(2, s_timeout);
        var rows = await WaitForRowsAsync(connectionString, r => r.Count == 1);

        // assert
        Assert.Equal([1, 2], recorder.Events.Select(e => e.Index).Order());
        Assert.Single(rows);
    }

    [Fact]
    public async Task Outbox_Should_DeliverWholeTransactionInOrder_When_LargerThanBatchSize()
    {
        // arrange
        const int count = (2 * PostgresMessageOutboxQueries.BatchSize) + 10;
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var provider = await CreateProviderAsync(connectionString, recorder, new DispatchListener());
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            for (var i = 0; i < count; i++)
            {
                await bus.PublishAsync(new SequencedEvent { Transaction = 0, Index = i }, TestToken);
            }

            await transaction.CommitAsync(TestToken);
        }

        await recorder.WaitForAsync(count, s_timeout);

        // assert
        Assert.Equal(Enumerable.Range(0, count), recorder.Events.Select(e => e.Index));
    }

    [Fact]
    public async Task Outbox_Should_DeliverEachTransactionInOrder_When_TwoProcessorsShareOneTable()
    {
        // arrange
        const int transactions = 20;
        const int perTransaction = 10;
        var connectionString = await fixture.CreateDatabaseAsync();
        var listener = new DispatchListener();
        var providerA = await CreateProviderAsync(connectionString, new EventRecorder(), listener);
        var providerB = await CreateProviderAsync(connectionString, new EventRecorder(), listener);
        for (var t = 0; t < transactions; t++)
        {
            var transactionId = Guid.NewGuid();
            for (var i = 0; i < perTransaction; i++)
            {
                await InsertAsync(connectionString, providerA, transactionId, t, i, unresolvable: false);
            }
        }

        // act
        await using var hostA = await MessagingTestHost.StartAsync(providerA, TestToken);
        await using var hostB = await MessagingTestHost.StartAsync(providerB, TestToken);
        await listener.WaitForAsync(transactions * perTransaction, s_timeout);

        // assert
        var dispatched = listener.MessageIds.ToList();
        var outOfOrder = dispatched
            .Select(id => id.Split(':'))
            .GroupBy(parts => parts[0], parts => int.Parse(parts[1]))
            .Where(group => !group.SequenceEqual(Enumerable.Range(0, perTransaction)))
            .Select(group => group.Key)
            .ToList();
        Assert.Equal(transactions * perTransaction, dispatched.Count);
        Assert.Equal(transactions * perTransaction, dispatched.Distinct().Count());
        Assert.Empty(outOfOrder);
    }

    [Fact]
    public async Task Outbox_Should_KeepTransactionRows_When_FirstMessageReachesAttemptLimit()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new EventRecorder();
        var provider = await CreateProviderAsync(connectionString, recorder, new DispatchListener());
        var transactionId = Guid.NewGuid();
        await InsertAsync(
            connectionString,
            provider,
            transactionId,
            0,
            0,
            unresolvable: true,
            timesSent: PostgresMessageOutboxQueries.MaxAttempts - 1);
        await InsertAsync(connectionString, provider, transactionId, 0, 1, unresolvable: false);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var rows = await WaitForRowsAsync(
            connectionString,
            r => r.Count == 2 && r[0].TimesSent == PostgresMessageOutboxQueries.MaxAttempts);

        // assert
        Assert.Equal(
            [PostgresMessageOutboxQueries.MaxAttempts, 0],
            rows.Select(r => r.TimesSent));
        Assert.Empty(recorder.Events);
    }

    private async Task<List<(Guid Id, Guid? TransactionId, int TimesSent)>> WaitForRowsAsync(
        string connectionString,
        Func<List<(Guid Id, Guid? TransactionId, int TimesSent)>, bool> predicate)
    {
        var deadline = DateTimeOffset.UtcNow + s_timeout;

        while (true)
        {
            var rows = await MessageRows.ReadOutboxAsync(connectionString, TestToken);

            if (predicate(rows) || DateTimeOffset.UtcNow >= deadline)
            {
                return rows;
            }

            await Task.Delay(50, TestToken);
        }
    }

    private static Task<Guid> InsertAsync(
        string connectionString,
        IServiceProvider provider,
        Guid? transactionId,
        int transaction,
        int index,
        bool unresolvable,
        int timesSent = 0)
        => MessageRows.InsertOutboxAsync(
            connectionString,
            CreateEnvelope(provider, $"{transaction}:{index}", transaction, index, unresolvable),
            transactionId,
            timesSent,
            TimeSpan.Zero,
            TestToken);

    private static MessageEnvelope CreateEnvelope(
        IServiceProvider provider,
        string messageId,
        int transaction,
        int index,
        bool unresolvable)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(SequencedEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = unresolvable ? "urn:message:unknown" : type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = unresolvable
                ? "unknown://destination"
                : runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new SequencedEvent { Transaction = transaction, Index = index })
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        EventRecorder recorder,
        DispatchListener listener)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddSingleton<IMessagingDiagnosticEventListener>(listener);
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(options => options.UsePostgresOutbox());
        builder.AddEventHandler<SequencedEventHandler>();

        // a single consumer keeps the received order equal to the order the outbox dispatches in
        builder.AddInMemory(t => t.Endpoint("sequenced").Handler<SequencedEventHandler>().MaxConcurrency(1));

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed class SequencedEvent
    {
        public required int Transaction { get; init; }

        public required int Index { get; init; }
    }

    public sealed class SequencedEventHandler(EventRecorder recorder) : IEventHandler<SequencedEvent>
    {
        public ValueTask HandleAsync(SequencedEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class EventRecorder
    {
        private readonly ConcurrentQueue<SequencedEvent> _events = [];

        public IEnumerable<SequencedEvent> Events => _events;

        public void Record(SequencedEvent message) => _events.Enqueue(message);

        public async Task WaitForAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;

            while (_events.Count < count && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(25, TestToken);
            }
        }
    }

    public sealed class DispatchListener : MessagingDiagnosticEventListener
    {
        private int _dispatches;

        public bool ThrowOnFirstDispatch { get; init; }

        public ConcurrentQueue<string> MessageIds { get; } = [];

        public override IDisposable Dispatch(IDispatchContext context)
        {
            // Publishing a message also dispatches it to the outbox, before an envelope exists.
            if (context.Envelope?.MessageId is { } messageId)
            {
                MessageIds.Enqueue(messageId);
            }

            if (ThrowOnFirstDispatch && Interlocked.Increment(ref _dispatches) == 1)
            {
                throw new InvalidOperationException("The first delivery failed deliberately.");
            }

            return EmptyScope;
        }

        public async Task WaitForAsync(int count, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;

            while (MessageIds.Count < count && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(25, TestToken);
            }
        }
    }
}
