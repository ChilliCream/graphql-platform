using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Events;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresConsumerTransactionTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UseTransaction_Should_OpenTransactionOnItsContext_When_AnotherContextIsRegisteredLater()
    {
        // arrange
        await using var host = await StartHostAsync(b =>
        {
            b.AddEntityFramework<FirstDbContext>(ef => ef.UseTransaction());
            b.AddEntityFramework<SecondDbContext>(_ => { });
            b.AddRequestHandler<InspectTransactionsHandler>();
        });
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var state = await bus.RequestAsync(new InspectTransactions(), TestToken)
            .AsTask()
            .WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal((true, false), (state.FirstHasTransaction, state.SecondHasTransaction));
    }

    [Fact]
    public async Task UseTransaction_Should_OpenTransactionOnEachContext_When_BothContextsUseTransaction()
    {
        // arrange
        await using var host = await StartHostAsync(b =>
        {
            b.AddEntityFramework<FirstDbContext>(ef => ef.UseTransaction());
            b.AddEntityFramework<SecondDbContext>(ef => ef.UseTransaction());
            b.AddRequestHandler<InspectTransactionsHandler>();
        });
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var state = await bus.RequestAsync(new InspectTransactions(), TestToken)
            .AsTask()
            .WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal((true, true), (state.FirstHasTransaction, state.SecondHasTransaction));
    }

    [Fact]
    public async Task UseTransaction_Should_RollBackItsContext_When_HandlerThrows()
    {
        // arrange
        var recorder = new OrderRecorder();
        await using var host = await StartHostAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEntityFramework<FirstDbContext>(ef => ef.UseTransaction());
            b.AddEntityFramework<SecondDbContext>(_ => { });
            b.AddRequestHandler<FailingPlaceOrderHandler>();
        });
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await Assert.ThrowsAsync<RemoteErrorException>(
            async () => await bus.RequestAsync(new PlaceOrder(), TestToken).AsTask().WaitAsync(s_timeout, TestToken));

        // assert
        Assert.Equal((1, 0), (recorder.RowsSeenByHandler, await CountOrdersAsync(host.Services)));
    }

    [Fact]
    public async Task PublishAsync_Should_Throw_When_OutboxIsOnAnotherContextThanUseTransaction()
    {
        // arrange
        var recorder = new PublishRecorder();
        await using var host = await StartHostAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEntityFramework<FirstDbContext>(ef => ef.UseTransaction());
            b.AddEntityFramework<SecondDbContext>(ef => ef.UsePostgresOutbox());
            b.AddEventHandler<StartOrderHandler>();
        });
        await using var scope = host.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var second = scope.ServiceProvider.GetRequiredService<SecondDbContext>();

        // act
        await using (var transaction = await second.Database.BeginTransactionAsync(TestToken))
        {
            await bus.PublishAsync(new StartOrder(), TestToken);
            await transaction.CommitAsync(TestToken);
        }

        var exception = await recorder.PublishOutcome.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(
            "The outbox for DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresConsumerTransactionTests+SecondDbContext' "
            + "requires a transaction to publish or send a message. "
            + "Begin a transaction on that DbContext, use UseTransaction() for consumers, "
            + "or set RequireTransaction to false in UsePostgresOutbox().",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    private async Task<MessagingTestHost> StartHostAsync(Action<IMessageBusHostBuilder> configure)
    {
        var firstConnectionString = await fixture.CreateDatabaseAsync();
        var secondConnectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddDbContext<FirstDbContext>(o => o.UseTestNpgsql(firstConnectionString));
        services.AddDbContext<SecondDbContext>(o => o.UseTestNpgsql(secondConnectionString));

        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<FirstDbContext>().Database.EnsureCreatedAsync(TestToken);
            await scope.ServiceProvider.GetRequiredService<SecondDbContext>().Database.EnsureCreatedAsync(TestToken);
        }

        return await MessagingTestHost.StartAsync(provider, TestToken);
    }

    private static async Task<int> CountOrdersAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FirstDbContext>();
        return await db.Orders.CountAsync(TestToken);
    }

    public sealed class FirstDbContext(DbContextOptions<FirstDbContext> options) : DbContext(options)
    {
        public DbSet<OrderRow> Orders => Set<OrderRow>();
    }

    public sealed class SecondDbContext(DbContextOptions<SecondDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    public sealed class OrderRow
    {
        public int Id { get; set; }
    }

    public sealed class InspectTransactions : IEventRequest<TransactionState>;

    public sealed record TransactionState(bool FirstHasTransaction, bool SecondHasTransaction);

    public sealed class InspectTransactionsHandler(FirstDbContext first, SecondDbContext second)
        : IEventRequestHandler<InspectTransactions, TransactionState>
    {
        public ValueTask<TransactionState> HandleAsync(
            InspectTransactions request,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(
                new TransactionState(
                    first.Database.CurrentTransaction is not null,
                    second.Database.CurrentTransaction is not null));
    }

    public sealed class PlaceOrder : IEventRequest<OrderPlaced>;

    public sealed class OrderPlaced;

    public sealed class OrderRecorder
    {
        public int RowsSeenByHandler { get; set; }
    }

    public sealed class FailingPlaceOrderHandler(FirstDbContext first, OrderRecorder recorder)
        : IEventRequestHandler<PlaceOrder, OrderPlaced>
    {
        public async ValueTask<OrderPlaced> HandleAsync(PlaceOrder request, CancellationToken cancellationToken)
        {
            first.Orders.Add(new OrderRow());
            await first.SaveChangesAsync(cancellationToken);
            recorder.RowsSeenByHandler = await first.Orders.CountAsync(cancellationToken);
            throw new InvalidOperationException("The order handler failed deliberately.");
        }
    }

    public sealed class StartOrder;

    public sealed class OrderStarted;

    public sealed class PublishRecorder
    {
        public TaskCompletionSource<Exception?> PublishOutcome { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class StartOrderHandler(IMessageBus bus, PublishRecorder recorder) : IEventHandler<StartOrder>
    {
        public async ValueTask HandleAsync(StartOrder message, CancellationToken cancellationToken)
        {
            try
            {
                await bus.PublishAsync(new OrderStarted(), cancellationToken);
                recorder.PublishOutcome.TrySetResult(null);
            }
            catch (Exception ex)
            {
                recorder.PublishOutcome.TrySetResult(ex);
            }
        }
    }
}
