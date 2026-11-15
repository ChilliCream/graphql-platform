using System.Collections.Concurrent;
using System.Text.Json;
using CookieCrumble;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowOnSave_When_NoTransactionIsActive()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new RowCountSignal(connectionString);
        await using var provider = await CreateProviderAsync(connectionString, signal);
        await EnsureCreatedAsync(provider);
        signal.Events.Clear();
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox(bus).PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);
        signal.Mark("published");
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(["published(rows=0)", "set(rows=1)"], signal.Events.Distinct().ToArray());
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowOnSave_When_SaveChangesIsSynchronous()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox(bus).PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            db.SaveChanges();
        }

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainMessageForRetry_When_SaveFails()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateProviderAsync(connectionString, signal);
        await EnsureCreatedAsync(provider);
        await SaveOrderAsync(provider, 1);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var order = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);

        // act
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestToken));
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        var failedSave = (
            await CountOutboxRowsAsync(connectionString),
            message.State,
            message.Entity.Envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
        order.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, 1), failedSave);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowWithTransaction_When_SavedInsideTransaction()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        // act
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
        await db.Outbox(bus).PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);
        await db.SaveChangesAsync(TestToken);
        var rowsBeforeCommit = await CountOutboxRowsAsync(connectionString);
        await transaction.CommitAsync(TestToken);
        var rowsAfterCommit = await CountOutboxRowsAsync(connectionString);

        // assert
        Assert.Equal((0L, 1L), (rowsBeforeCommit, rowsAfterCommit));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowOnSave_When_DbContextIsPooled()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true);
        await EnsureCreatedAsync(provider);

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox(bus).PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ClearMessages_When_PooledDbContextIsReturned()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true);
        await EnsureCreatedAsync(provider);
        UnitOfWorkDbContext leased;
        await using (var scope = provider.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            leased = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await leased.Outbox(bus).PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
        }

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            Assert.Same(leased, db);
            await db.SaveChangesAsync(TestToken);
        }

        // assert
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public async Task OutboxPublishAsync_Should_SaveMessage_When_ContextIsCreatedDirectlyByFactory(
        bool pooled,
        bool asynchronous,
        bool outboxFirst)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            outboxFirst: outboxFirst);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = asynchronous
            ? await factory.CreateDbContextAsync(TestToken)
            : factory.CreateDbContext();

        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        if (asynchronous)
        {
            await db.SaveChangesAsync(TestToken);
        }
        else
        {
            db.SaveChanges();
        }

        // assert
        Assert.Equal((1L, 1), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_RejectPreviousHandle_When_PooledContextIsReused(bool asynchronous)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), pooled: true, useFactory: true);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var first = await factory.CreateDbContextAsync(TestToken);
        var previousOutbox = first.Outbox(bus);
        await previousOutbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        if (asynchronous)
        {
            await first.DisposeAsync();
        }
        else
        {
            first.Dispose();
        }

        await using var second = await factory.CreateDbContextAsync(TestToken);

        // act
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await previousOutbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken));
        await second.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
        await second.SaveChangesAsync(TestToken);
        using var stored = await second.Set<OutboxMessage>().Select(message => message.Envelope).SingleAsync(TestToken);

        // assert
        Assert.Same(first, second);
        Assert.Equal(3, stored.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_OwnMessagesPerContext_When_FactoryCreatesMultipleContexts(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var first = await factory.CreateDbContextAsync(TestToken);
        var second = await factory.CreateDbContextAsync(TestToken);

        first.Orders.Add(new Order { Id = 1 });
        second.Orders.Add(new Order { Id = 2 });
        await first.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await second.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);

        // act
        await first.SaveChangesAsync(TestToken);
        await second.DisposeAsync();
        var savedOrderIds = await first.Orders.Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal([1], savedOrderIds);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_PreserveIndependentCommitAndCorrelation_When_HandlerFails()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        var handlerRecorder = new FactoryHandlerRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true,
            useFactory: true,
            scopeRecorder: recorder,
            handlerRecorder: handlerRecorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var outbox = new CapturingOutbox();
        await ((DefaultMessageBus)bus).PublishAsync(
            new StartFactoryWork(), PublishOptions.Default, outbox, TestToken);
        var incoming = outbox.Envelope;
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = (InMemoryDispatchEndpoint)runtime.GetDispatchEndpoint(
            new Uri(incoming.DestinationAddress!));
        await endpoint.Topic!.SendAsync(incoming, TestToken);
        await handlerRecorder.Completed.Task.WaitAsync(s_timeout, TestToken);
        var messages = recorder.Messages.ToArray();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = await factory.CreateDbContextAsync(TestToken);

        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
        var outsideHandler = recorder.Messages.Last();
        var savedOrderIds = await db.Orders.Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal([1], savedOrderIds);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(
            Enumerable.Repeat<(string? ConversationId, string? CausationId)>(
                ("factory-conversation", "factory-message"), 2),
            messages.Select(m => (m.ConversationId, m.CausationId)));
        Assert.Equal(
            (false, null),
            (outsideHandler.ConversationId == "factory-conversation", outsideHandler.CausationId));
    }

    [Fact]
    public async Task OutboxDispatch_Should_AllowNextOperation_When_PreviousOperationWasAlreadyCancelled()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var cancelledToken = new CancellationToken(canceled: true);

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, cancelledToken));
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await db.SaveChangesAsync(TestToken);
        using var envelope = await db.Set<OutboxMessage>().Select(x => x.Envelope).SingleAsync(TestToken);

        // assert
        Assert.Equal(2, envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowWithSave_When_PublishedFromSavingChangesInterceptor()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var interceptor = new PublishOnSaveInterceptor();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            interceptor: interceptor);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        db.MessageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 1 });

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_SaveOnlyLaterMessage_When_ChangeTrackerIsClearedBeforeSave()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        db.ChangeTracker.Clear();

        // act
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await db.SaveChangesAsync(TestToken);
        using var stored = await db.Set<OutboxMessage>().Select(x => x.Envelope).SingleAsync(TestToken);

        // assert
        Assert.Equal(2, stored.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_NotRestoreMessage_When_TransactionRollsBackAfterAcceptedSave()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var order = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(TestToken);
            await transaction.RollbackAsync(TestToken);
        }

        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, 0), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
        Assert.Equal(EntityState.Unchanged, order.State);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainMessageForRetry_When_TransactionRollsBackBeforeAcceptance()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
            await transaction.RollbackAsync(TestToken);
        }

        var rolledBackState = (
            await CountOutboxRowsAsync(connectionString),
            message.State,
            message.Entity.Envelope.RootElement.ValueKind);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, JsonValueKind.Object), rolledBackState);
        Assert.Equal((1L, 1), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_StageDomainEventOnce_When_SaveIsRetried(bool useInterceptor)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            interceptor: useInterceptor ? new PublishOnSaveInterceptor() : null);
        await EnsureCreatedAsync(provider);
        await SaveOrderAsync(provider, 1);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.PublishDomainEvents = !useInterceptor;
        var conflictingOrder = db.Orders.Add(new Order { Id = 1 });
        db.Orders.Add(new Order { Id = 2 });
        db.MessageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 2 });

        // act
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestToken));
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        var failedSave = (await CountOutboxRowsAsync(connectionString), message.State, db.PendingEvents.Count);
        conflictingOrder.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, 0), failedSave);
        Assert.Equal((1L, 2), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
    }

    [Fact]
    public async Task Outbox_Should_Throw_When_DbContextHasNoOutbox()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var db = new UnitOfWorkDbContext(
            new DbContextOptionsBuilder<UnitOfWorkDbContext>().UseTestNpgsql(connectionString).Options);

        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => db.Outbox(bus));

        // assert
        Assert.Equal(
            $"The DbContext '{typeof(UnitOfWorkDbContext).FullName}' has no outbox. "
            + $"Register one with AddEntityFramework<{nameof(UnitOfWorkDbContext)}>(x => x.UsePostgresOutbox()) "
            + "and create the context through dependency injection (AddDbContext, AddDbContextFactory, "
            + "AddDbContextPool or AddPooledDbContextFactory); a context constructed by hand is not supported.",
            exception.Message);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_SaveEntityAndRelayMessage_When_PublishedFromSaveChangesOverride()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new MessageRecorder();
        await using var provider = await CreateProviderAsync(connectionString, new ResilientOutboxSignal(), recorder);
        await EnsureCreatedAsync(provider);
        var hostedServices = await StartHostedServicesAsync(provider);

        try
        {
            // act
            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
                db.PublishDomainEvents = true;
                db.Orders.Add(new Order { Id = 42 });
                db.MessageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 42 });
                await db.SaveChangesAsync(TestToken);
            }

            var delivered = await recorder.WaitAsync(s_timeout);
            var drained = await WaitForEmptyOutboxAsync(connectionString);

            // assert
            Assert.True(delivered, "message published from the SaveChanges override was not delivered");
            Assert.True(drained, "relayed outbox row was not deleted");
            Assert.Equal([42], recorder.OrderIds);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_FreezePayload_When_MessageIsChangedBeforeSave()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var message = new OrderPlaced { OrderId = 1 };

        // act
        await db.Outbox(bus).PublishAsync(message, TestToken);
        message.OrderId = 2;
        await db.Outbox(bus).PublishAsync(message, TestToken);
        await db.SaveChangesAsync(TestToken);
        var envelopes = await db.Set<OutboxMessage>().OrderBy(x => x.Id)
            .Select(x => x.Envelope).ToArrayAsync(TestToken);
        var values = envelopes.Select(envelope =>
        {
            using (envelope)
            {
                return envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId;
            }
        }).ToArray();

        // assert
        Assert.Equal([1, 2], values);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_RejectNonAtomicSave_When_AutomaticTransactionsAreDisabled()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestToken));
        var pending = db.ChangeTracker.Entries<OutboxMessage>().Single().State;
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(EntityState.Added, pending);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        var orderIds = await db.Orders.Select(x => x.Id).ToArrayAsync(TestToken);
        Assert.Equal([1], orderIds);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OutboxPublishAsync_Should_UseSuppliedBus_When_ContextHasMultipleOutboxHandles(
        bool useFactory,
        bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: useFactory,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var other = provider.CreateAsyncScope();
        await using var db = useFactory
            ? await caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
                .CreateDbContextAsync(TestToken)
            : caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var callerMarker = caller.ServiceProvider.GetRequiredService<ScopeMarker>();
        var otherMarker = other.ServiceProvider.GetRequiredService<ScopeMarker>();
        var first = db.Outbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());
        var second = db.Outbox(other.ServiceProvider.GetRequiredService<IMessageBus>());

        // act
        await first.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await second.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await first.PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal([callerMarker, otherMarker, callerMarker], recorder.Messages.Select(message => message.Scope));
        Assert.Equal(3, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_SaveMessage_When_ContextUsesAnEquivalentModelInstance()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: true);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var registered = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await using var modelContext = new UnitOfWorkDbContext(
            new DbContextOptionsBuilder<UnitOfWorkDbContext>().UseTestNpgsql(connectionString).Options);
        var options = new DbContextOptionsBuilder<UnitOfWorkDbContext>(
            provider.GetRequiredService<DbContextOptions<UnitOfWorkDbContext>>())
            .UseModel(modelContext.Model)
            .Options;
        await using var db = new UnitOfWorkDbContext(options);

        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var beforeSave = await CountOutboxRowsAsync(connectionString);
        await db.SaveChangesAsync(TestToken);
        using var envelope = await registered.Set<OutboxMessage>().Select(message => message.Envelope).SingleAsync(TestToken);

        // assert
        new
        {
            SeparateModel = !ReferenceEquals(registered.Model, db.Model),
            BeforeSave = beforeSave,
            OrderId = await registered.Orders.Select(order => order.Id).SingleAsync(TestToken),
            MessageOrderId = envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId
        }.MatchInlineSnapshot(
            """
            {
              "SeparateModel": true,
              "BeforeSave": 0,
              "OrderId": 1,
              "MessageOrderId": 1
            }
            """);
    }

    [Fact]
    public async Task OutboxSendAsync_Should_SaveDirectedMessage_When_PublishedThroughOutbox()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new MessageRecorder();
        await using var provider = await CreateProviderAsync(connectionString, new ResilientOutboxSignal(), recorder);
        await EnsureCreatedAsync(provider);
        var hosted = await StartHostedServicesAsync(provider);

        try
        {
            // act
            await using (var caller = provider.CreateAsyncScope())
            {
                var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
                var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
                await db.Outbox(bus).SendAsync(new FulfillOrder(42), TestToken);
                await db.SaveChangesAsync(TestToken);
            }

            var delivered = await recorder.WaitAsync(s_timeout);
            var drained = await WaitForEmptyOutboxAsync(connectionString);

            // assert
            Assert.True(delivered);
            Assert.True(drained);
            Assert.Equal([42], recorder.OrderIds);
        }
        finally
        {
            await StopHostedServicesAsync(hosted);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChangesAsync_Should_AllowNonOutboxSave_When_StagedMessagesAreClearedOrAccepted(bool accept)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        if (accept)
        {
            await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
            db.ChangeTracker.AcceptAllChanges();
        }
        else
        {
            db.ChangeTracker.Clear();
        }

        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);
        var orders = await db.Orders.Select(x => x.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal([2], orders);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(accept ? 1 : 0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_RejectNonAtomicStaging_When_PublishedDirectlyOrDuringSave(bool duringSave)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), interceptor: new PublishOnSaveInterceptor());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        db.Orders.Add(new Order { Id = 1 });
        db.MessageBus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 1 });

        // act
        if (duringSave)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestToken));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        }

        var orders = await db.Orders.CountAsync(TestToken);

        // assert
        Assert.Equal((0L, 0), (await CountOutboxRowsAsync(connectionString), orders));
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        return new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
    }

    private static async Task SaveOrderAsync(IServiceProvider provider, int id)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = id });
        await db.SaveChangesAsync(TestToken);
    }

    private static async Task EnsureCreatedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        if (scope.ServiceProvider.GetService<IDbContextFactory<UnitOfWorkDbContext>>() is { } factory)
        {
            await using var db = await factory.CreateDbContextAsync(TestToken);
            await db.Database.EnsureCreatedAsync(TestToken);
        }
        else
        {
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Database.EnsureCreatedAsync(TestToken);
        }
    }

    private static async Task<long> CountOutboxRowsAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static async Task<bool> WaitForEmptyOutboxAsync(string connectionString)
    {
        var deadline = DateTimeOffset.UtcNow + s_timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await CountOutboxRowsAsync(connectionString) == 0)
            {
                return true;
            }

            await Task.Delay(50, TestToken);
        }

        return false;
    }

    private static async Task<List<IHostedService>> StartHostedServicesAsync(IServiceProvider provider)
    {
        var hostedServices = provider.GetServices<IHostedService>()
            .Where(s => s is not MessagingRuntimeHostedService)
            .ToList();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(TestToken);
        }

        return hostedServices;
    }

    private static async Task StopHostedServicesAsync(List<IHostedService> hostedServices)
    {
        foreach (var service in hostedServices)
        {
            await service.StopAsync(CancellationToken.None);
        }

        // Allow pending commits to finish.
        await Task.Delay(250, CancellationToken.None);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        IOutboxSignal signal,
        MessageRecorder? recorder = null,
        bool pooled = false,
        SaveChangesInterceptor? interceptor = null,
        bool useFactory = false,
        ScopeRecorder? scopeRecorder = null,
        bool outboxFirst = false,
        FactoryHandlerRecorder? handlerRecorder = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder ?? new MessageRecorder());
        services.AddLogging();
        services.AddSingleton(signal);

        if (scopeRecorder is not null)
        {
            services.AddSingleton(scopeRecorder);
            services.AddScoped<ScopeMarker>();
        }

        var builder = services.AddMessageBus();
        if (outboxFirst)
        {
            builder.AddEntityFramework<UnitOfWorkDbContext>(ef => ef.UsePostgresOutbox());
        }

        if (useFactory)
        {
            if (pooled)
            {
                services.AddPooledDbContextFactory<UnitOfWorkDbContext>(o => o.UseTestNpgsql(connectionString));
            }
            else
            {
                services.AddDbContextFactory<UnitOfWorkDbContext>(o => o.UseTestNpgsql(connectionString));
            }
        }
        else if (pooled)
        {
            services.AddDbContextPool<UnitOfWorkDbContext>(o => o.UseTestNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<UnitOfWorkDbContext>(o =>
            {
                o.UseTestNpgsql(connectionString);
                if (interceptor is not null)
                {
                    o.AddInterceptors(interceptor);
                }
            });
        }

        if (!outboxFirst)
        {
            builder.AddEntityFramework<UnitOfWorkDbContext>(ef => ef.UsePostgresOutbox());
        }

        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddRequestHandler<FulfillOrderHandler>();
        builder.AddInMemory();

        if (handlerRecorder is not null)
        {
            services.AddSingleton(handlerRecorder);
            builder.AddEventHandler<FactoryPublishingHandler>();
            builder.AddEntityFramework<UnitOfWorkDbContext>(ef => ef.UseTransaction());
            builder.AddResilience(p => p.On<FactoryHandlerException>().Discard());
            builder.ConfigureMessageBus(bus => bus.UseConsume(
                new ConsumerMiddlewareConfiguration(
                    static (_, next) => async context =>
                    {
                        try
                        {
                            await next(context);
                        }
                        catch (FactoryHandlerException)
                        {
                            context.Services.GetRequiredService<FactoryHandlerRecorder>().Completed.SetResult();
                            throw;
                        }
                    },
                    "FactoryFailureRecorder"),
                after: "Retry"));
        }

        if (scopeRecorder is not null)
        {
            builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => async context =>
                    {
                        if (context.Message is StartFactoryWork)
                        {
                            context.MessageId = "factory-message";
                            context.ConversationId = "factory-conversation";
                        }
                        else
                        {
                            context.Services.GetRequiredService<ScopeRecorder>().Messages.Enqueue((
                                context.Services.GetRequiredService<ScopeMarker>(),
                                context.ConversationId,
                                context.CausationId));
                        }

                        await next(context);
                    },
                    "ScopeRecorder")));
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);

        if (pooled)
        {
            // Complete table metadata discovery before testing context leases.
            provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
                .Get(typeof(UnitOfWorkDbContext).FullName);
        }

        return provider;
    }

    public sealed class UnitOfWorkDbContext(DbContextOptions<UnitOfWorkDbContext> options) : DbContext(options)
    {
        public bool PublishDomainEvents { get; set; }

        public IMessageBus? MessageBus { get; set; }

        public Queue<OrderPlaced> PendingEvents { get; } = new();

        public DbSet<Order> Orders => Set<Order>();

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (PublishDomainEvents)
            {
                await PublishDomainEventsAsync(cancellationToken);
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public async ValueTask PublishDomainEventsAsync(CancellationToken cancellationToken)
        {
            while (PendingEvents.TryPeek(out var domainEvent))
            {
                await this.Outbox(MessageBus!).PublishAsync(domainEvent, cancellationToken);
                PendingEvents.Dequeue();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<Order>().ToTable("orders").Property(o => o.Id).ValueGeneratedNever();
        }
    }

    private sealed class PublishOnSaveInterceptor : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await ((UnitOfWorkDbContext)eventData.Context!).PublishDomainEventsAsync(cancellationToken);
            return result;
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    private sealed class ScopeMarker;

    private sealed class ScopeRecorder
    {
        public ConcurrentQueue<(ScopeMarker Scope, string? ConversationId, string? CausationId)> Messages { get; } = new();
    }

    private sealed class CapturingOutbox : IMessageOutbox
    {
        public MessageEnvelope Envelope { get; private set; } = null!;

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            Envelope = MessageEnvelope.CopyWithOwnedBody(envelope);
            return default;
        }
    }

    public sealed class StartFactoryWork;

    public sealed class FactoryHandlerRecorder
    {
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class FactoryHandlerException : Exception;

    public sealed class FactoryPublishingHandler(
        IDbContextFactory<UnitOfWorkDbContext> factory,
        IServiceProvider services,
        IMessageBus bus) : IEventHandler<StartFactoryWork>
    {
        public async ValueTask HandleAsync(StartFactoryWork message, CancellationToken cancellationToken)
        {
            var managed = services.GetRequiredService<UnitOfWorkDbContext>();
            managed.Orders.Add(new Order { Id = 3 });
            await managed.SaveChangesAsync(cancellationToken);
            await using var first = await factory.CreateDbContextAsync(cancellationToken);
            await using var second = await factory.CreateDbContextAsync(cancellationToken);
            first.Orders.Add(new Order { Id = 1 });
            second.Orders.Add(new Order { Id = 2 });
            await first.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, cancellationToken);
            await second.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 2 }, cancellationToken);
            await first.SaveChangesAsync(cancellationToken);
            throw new FactoryHandlerException();
        }
    }

    public sealed record FulfillOrder(int OrderId) : IEventRequest;

    public sealed class FulfillOrderHandler(MessageRecorder recorder) : IEventRequestHandler<FulfillOrder>
    {
        public ValueTask HandleAsync(FulfillOrder message, CancellationToken cancellationToken)
        {
            recorder.Record(message.OrderId);
            return default;
        }
    }

    public sealed class OrderPlaced
    {
        public required int OrderId { get; set; }
    }

    public sealed class OrderPlacedHandler(MessageRecorder recorder) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            recorder.Record(message.OrderId);
            return default;
        }
    }

    public sealed class MessageRecorder
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public ConcurrentQueue<int> OrderIds { get; } = new();

        public void Record(int orderId)
        {
            OrderIds.Enqueue(orderId);
            _semaphore.Release();
        }

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphore.WaitAsync(timeout);
    }

    private sealed class RowCountSignal(string connectionString) : IOutboxSignal
    {
        private readonly string _connectionString =
            new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false }.ConnectionString;

        public ConcurrentQueue<string> Events { get; } = new();

        public void Mark(string label) => Events.Enqueue($"{label}(rows={CountRows()})");

        public void Set() => Events.Enqueue($"set(rows={CountRows()})");

        public Task WaitAsync(CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);

        private object? CountRows()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
            return command.ExecuteScalar();
        }
    }
}
