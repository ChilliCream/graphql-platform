#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
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
        var rowsAfterFailedSave = await CountOutboxRowsAsync(connectionString);
        var stateAfterFailedSave = message.State;
        var stagedOrderId = message.Entity.Envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId;
        order.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(0, rowsAfterFailedSave);
        Assert.Equal(EntityState.Added, stateAfterFailedSave);
        Assert.Equal(1, stagedOrderId);
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
        Assert.Equal(0, rowsBeforeCommit);
        Assert.Equal(1, rowsAfterCommit);
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
        Assert.Equal([1], await ReadStoredOrderIdsAsync(connectionString));
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
        Assert.Equal([1], await ReadStoredOrderIdsAsync(connectionString));
        Assert.Equal(1, await db.Orders.CountAsync(TestToken));
        Assert.Equal([EntityState.Unchanged], db.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.State));
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

    [Fact]
    public async Task OutboxPublishAsync_Should_ThrowAndStageNothing_When_PooledContextIsReusedDuringDispatch()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true,
            useFactory: true,
            configureBus: builder => builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        entered.TrySetResult();
                        await release.Task;
                        await next(context);
                    },
                    "PauseDispatch"))));
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var first = await factory.CreateDbContextAsync(TestToken);
        var pending = first.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken).AsTask();
        await entered.Task.WaitAsync(s_timeout, TestToken);
        await first.DisposeAsync();
        await using var second = await factory.CreateDbContextAsync(TestToken);
        second.Orders.Add(new Order { Id = 2 });

        // act
        release.SetResult();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => pending.WaitAsync(s_timeout, TestToken));
        await second.SaveChangesAsync(TestToken);
        var savedOrders = await second.Orders.Select(order => order.Id).ToArrayAsync(TestToken);
        var storedMessages = await ReadStoredOrderIdsAsync(connectionString);

        // assert
        Assert.Same(first, second);
        Assert.Equal([2], savedOrders);
        Assert.Empty(storedMessages);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OutboxDispatch_Should_StageNothing_When_MiddlewareThrowsAfterNext(bool send, bool scheduled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var fail = false;
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            configureBus: builder => builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => async context =>
                    {
                        await next(context);
                        if (fail)
                        {
                            throw new InvalidOperationException("after next");
                        }
                    },
                    "FailAfterNext"))));
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var outbox = db.Outbox(bus);
        var scheduledTime = scheduled ? new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero) : (DateTimeOffset?)null;
        await outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        db.Orders.Add(new Order { Id = 2 });
        fail = true;

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (send)
            {
                await outbox.SendAsync(new FulfillOrder(3), new SendOptions { ScheduledTime = scheduledTime }, TestToken);
            }
            else
            {
                await outbox.PublishAsync(
                    new OrderPlaced { OrderId = 3 },
                    new PublishOptions { ScheduledTime = scheduledTime },
                    TestToken);
            }
        });
        await db.SaveChangesAsync(TestToken);
        var savedOrders = await db.Orders.Select(order => order.Id).ToArrayAsync(TestToken);
        var storedMessages = await ReadStoredOrderIdsAsync(connectionString);

        // assert
        Assert.Equal("after next", exception.Message);
        Assert.Equal([2], savedOrders);
        Assert.Equal([1], storedMessages);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ThrowBeforeDispatchMiddleware_When_ContextIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var middlewareCalls = 0;
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            configureBus: builder => builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    (_, next) => context =>
                    {
                        middlewareCalls++;
                        return next(context);
                    },
                    "CountDispatches"))));
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var outbox = db.Outbox(bus);
        await db.DisposeAsync();

        // act
        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Equal(
            "The DbContext owning this outbox has been disposed or returned to its pool."
            + Environment.NewLine
            + "Object name: 'DbContextOutbox'.",
            exception.Message);
        Assert.Equal(0, middlewareCalls);
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
        Assert.Equal([1], await ReadStoredOrderIdsAsync(connectionString));
        Assert.Equal([1], savedOrderIds);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_KeepFactoryContextCommit_When_HandlerFails()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new MessageRecorder();
        var handlerRecorder = new FactoryHandlerRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ResilientOutboxSignal(),
            recorder,
            pooled: true,
            useFactory: true,
            handlerRecorder: handlerRecorder);
        await EnsureCreatedAsync(provider);
        var hosted = await StartHostedServicesAsync(provider);

        try
        {
            // act
            await PublishStartFactoryWorkAsync(provider);
            await handlerRecorder.Completed.Task.WaitAsync(s_timeout, TestToken);
            var delivered = await recorder.WaitAsync(s_timeout);
            var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
            await using var db = await factory.CreateDbContextAsync(TestToken);
            var savedOrderIds = await db.Orders.Select(o => o.Id).ToArrayAsync(TestToken);

            // assert
            Assert.True(delivered);
            Assert.Equal([1], recorder.OrderIds);
            Assert.Equal([1], savedOrderIds);
        }
        finally
        {
            await StopHostedServicesAsync(hosted);
        }
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_TakeCorrelationFromHandler_When_PublishedInsideHandler()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        var handlerRecorder = new FactoryHandlerRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new ResilientOutboxSignal(),
            pooled: true,
            useFactory: true,
            scopeRecorder: recorder,
            handlerRecorder: handlerRecorder);
        await EnsureCreatedAsync(provider);
        var hosted = await StartHostedServicesAsync(provider);

        try
        {
            // act
            await PublishStartFactoryWorkAsync(provider);
            await handlerRecorder.Completed.Task.WaitAsync(s_timeout, TestToken);
            var insideHandler = recorder.Messages.ToArray();
            await using var scope = provider.CreateAsyncScope();
            var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
            await using var db = await factory.CreateDbContextAsync(TestToken);
            await db.Outbox(scope.ServiceProvider.GetRequiredService<IMessageBus>())
                .PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
            var outsideHandler = recorder.Messages.Last();

            // assert
            Assert.Equal(
                Enumerable.Repeat<(string? ConversationId, string? CausationId)>(
                    ("factory-conversation", "factory-message"), 2),
                insideHandler.Select(m => (m.ConversationId, m.CausationId)));
            Assert.NotEqual("factory-conversation", outsideHandler.ConversationId);
            Assert.Null(outsideHandler.CausationId);
        }
        finally
        {
            await StopHostedServicesAsync(hosted);
        }
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
        Assert.Equal([1], await ReadStoredOrderIdsAsync(connectionString));
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
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(0, await db.Orders.CountAsync(TestToken));
        Assert.Equal(EntityState.Unchanged, order.State);
        Assert.Equal([EntityState.Unchanged], db.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.State));
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

        var rowsAfterRollback = await CountOutboxRowsAsync(connectionString);
        var stateAfterRollback = message.State;
        var payloadAfterRollback = message.Entity.Envelope.RootElement.ValueKind;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(0, rowsAfterRollback);
        Assert.Equal(EntityState.Added, stateAfterRollback);
        Assert.Equal(JsonValueKind.Object, payloadAfterRollback);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(1, await db.Orders.CountAsync(TestToken));
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
        var stagedAfterFailedSave = db.ChangeTracker.Entries<OutboxMessage>().Count();
        var rowsAfterFailedSave = await CountOutboxRowsAsync(connectionString);
        conflictingOrder.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, stagedAfterFailedSave);
        Assert.Equal(0, rowsAfterFailedSave);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(2, await db.Orders.CountAsync(TestToken));
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
            + "AddDbContextPool or AddPooledDbContextFactory), or construct it with options resolved from "
            + "dependency injection; options built by hand are not supported.",
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

    [Fact]
    public async Task OutboxPublishAsync_Should_UseSuppliedBus_When_ContextHasMultipleOutboxHandles()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var other = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
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

    [Fact]
    public async Task SaveChangesAsync_Should_AllowNonOutboxSave_When_StagedMessagesAreCleared()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        db.ChangeTracker.Clear();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);
        var orders = await db.Orders.Select(x => x.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal([2], orders);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_AllowNonOutboxSave_When_StagedMessagesAreAccepted()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
        db.ChangeTracker.AcceptAllChanges();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);
        var orders = await db.Orders.Select(x => x.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal([2], orders);
        Assert.Equal([EntityState.Unchanged], db.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.State));
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
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
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(0, orders);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ShareEntityFrameworkServices_When_ManyHostsRunInOneProcess()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using (var setup = await CreateProviderAsync(connectionString, new StubOutboxSignal()))
        {
            await EnsureCreatedAsync(setup);
        }

        var modelSources = new HashSet<IModelSource>(ReferenceEqualityComparer.Instance);

        // act
        for (var i = 0; i < 25; i++)
        {
            await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
            await using var scope = provider.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = i }, TestToken);
            await db.SaveChangesAsync(TestToken);
            modelSources.Add(db.GetService<IModelSource>());
        }

        // assert
        Assert.Single(modelSources);
        Assert.Equal(25, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_Throw_When_MiddlewareSkipsOutbox()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            configureBus: builder => builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => context =>
                    {
                        context.SkipOutbox();
                        return next(context);
                    },
                    "SkipOutbox"))));
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await db.Outbox(bus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Equal(
            "SkipOutbox() cannot be used for a message published or sent through dbContext.Outbox(bus), "
            + "because the message would be sent before the DbContext transaction commits.",
            exception.Message);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_Throw_When_BusComesFromProviderWithoutOutbox()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        var otherServices = new ServiceCollection();
        otherServices.AddLogging();
        otherServices.AddMessageBus().AddInMemory();
        await using var other = otherServices.BuildServiceProvider();
        var otherRuntime = (MessagingRuntime)other.GetRequiredService<IMessagingRuntime>();
        await otherRuntime.StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await using var otherScope = other.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var otherBus = otherScope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await db.Outbox(otherBus).PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Equal(
            "The message bus was not resolved from the service provider that registers the outbox of the "
            + $"DbContext '{typeof(UnitOfWorkDbContext).FullName}'. Pass the IMessageBus resolved from that "
            + "service provider.",
            exception.Message);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        return new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
    }

    private static async Task PublishStartFactoryWorkAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = await factory.CreateDbContextAsync(TestToken);
        await db.Outbox(scope.ServiceProvider.GetRequiredService<IMessageBus>())
            .PublishAsync(new StartFactoryWork(), TestToken);
        await db.SaveChangesAsync(TestToken);
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

    private static async Task<IReadOnlyList<int>> ReadStoredOrderIdsAsync(string connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString) { Enlist = false };
        await using var connection = new NpgsqlConnection(settings.ConnectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT envelope::text FROM outbox_messages";
        await using var reader = await command.ExecuteReaderAsync(TestToken);
        var orderIds = new List<int>();

        while (await reader.ReadAsync(TestToken))
        {
            using var envelope = JsonDocument.Parse(reader.GetString(0));
            orderIds.Add(envelope.RootElement.GetProperty("body").Deserialize<OrderPlaced>()!.OrderId);
        }

        orderIds.Sort();
        return orderIds;
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
        FactoryHandlerRecorder? handlerRecorder = null,
        Action<IMessageBusHostBuilder>? configureBus = null)
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
#if !NET10_0_OR_GREATER
                // Before EF Core 10, AddPooledDbContextFactory does not register the context itself.
                services.AddScoped(
                    sp => sp.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>().CreateDbContext());
#endif
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

        configureBus?.Invoke(builder);

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
#endif
