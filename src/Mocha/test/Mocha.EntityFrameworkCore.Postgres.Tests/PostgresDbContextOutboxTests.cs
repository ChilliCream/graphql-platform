using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox.PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        signal.Mark("published");
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(["published(rows=0)", "set(rows=1)"], signal.Events.Distinct().ToArray());
        Assert.Equal(EntityState.Detached, message.State);
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(message.Entity.Envelope));
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
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox.PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            db.SaveChanges();
        }

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_NotWriteRow_When_DbContextIsNotSaved()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateProviderAsync(connectionString, signal);
        await EnsureCreatedAsync(provider);
        var setCountBefore = signal.SetCallCount;
        string envelope;

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox.PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            envelope = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;
        }

        // assert
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(setCountBefore, signal.SetCallCount);
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(envelope));
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
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var order = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);

        // act
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestToken));
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();
        var failedSave = (
            await CountOutboxRowsAsync(connectionString),
            message.State,
            GetEnvelopeKind(message.Entity.Envelope));
        order.State = EntityState.Detached;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, JsonValueKind.Object), failedSave);
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
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        // act
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
        await db.Outbox.PublishAsync(
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
        var createdCount = UnitOfWorkDbContext.CreatedCount;

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox.PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(createdCount, UnitOfWorkDbContext.CreatedCount);
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
        string envelope;
        await using (var scope = provider.CreateAsyncScope())
        {
            leased = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await leased.Outbox.PublishAsync(
                new OrderPlaced { OrderId = 1 },
                TestToken);
            envelope = leased.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;
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
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(envelope));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task OutboxPublishAsync_Should_ReuseCallerScope_When_ContextPublishesMultipleMessages(
        bool pooled,
        bool useFactory)
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
        var callerMarker = caller.ServiceProvider.GetRequiredService<ScopeMarker>();
        callerMarker.Name = "caller";
        await using var db = useFactory
            ? await caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
                .CreateDbContextAsync(TestToken)
            : caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        if (useFactory)
        {
            db.UseOutbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());
        }

        // act
        _ = db.Outbox;
        var scopesBeforePublish = recorder.Created.Count;
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        var messages = recorder.Messages.ToArray();
        await db.SaveChangesAsync(TestToken);
        await db.DisposeAsync();
        var callerDisposalsAfterContextDisposal = callerMarker.DisposeCount;
        // DI-pooled contexts return their lease when the owning scope is disposed.
        await caller.DisposeAsync();

        // assert
        Assert.Equal(1, scopesBeforePublish);
        Assert.Same(messages[0].Scope, messages[1].Scope);
        Assert.Same(callerMarker, messages[0].Scope);
        Assert.Equal(
            ("caller", 1, 0, 1),
            (messages[0].Scope.Name, messages[0].Scope.DisposeCount,
                callerDisposalsAfterContextDisposal, callerMarker.DisposeCount));
        Assert.Equal(2, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
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
        db.UseOutbox(bus);
        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
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
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_UseNewCallerScope_When_FactoryReusesPooledContext(bool asynchronous)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var firstScope = provider.CreateAsyncScope();
        var first = factory.CreateDbContext();
        first.UseOutbox(firstScope.ServiceProvider.GetRequiredService<IMessageBus>());
        await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var discarded = first.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;

        // act
        if (asynchronous)
        {
            await first.DisposeAsync();
        }
        else
        {
            first.Dispose();
        }

        await firstScope.DisposeAsync();
        await using var secondScope = provider.CreateAsyncScope();
        await using var second = factory.CreateDbContext();
        second.UseOutbox(secondScope.ServiceProvider.GetRequiredService<IMessageBus>());
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await second.SaveChangesAsync(TestToken);
        var messages = recorder.Messages.ToArray();

        // assert
        Assert.Same(first, second);
        Assert.NotSame(messages[0].Scope, messages[1].Scope);
        Assert.Equal((1, 0), (messages[0].Scope.DisposeCount, messages[1].Scope.DisposeCount));
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(discarded));
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposeAsync_Should_PreserveCallerServices_When_FactoryContextIsReleased(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder { UseAsyncMarker = true };
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var caller = provider.CreateAsyncScope();
        var db = await factory.CreateDbContextAsync(TestToken);
        db.UseOutbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var marker = recorder.AsyncMarkers.Single();

        // act
        await db.DisposeAsync();

        // assert
        Assert.Equal(0, marker.DisposeCount);
        Assert.Equal(0, recorder.Created.Single().DisposeCount);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_Throw_When_OwningContextIsDisposed(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var db = await factory.CreateDbContextAsync(TestToken);
        var outbox = db.Outbox;
        await db.DisposeAsync();

        // act
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Empty(recorder.Created);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_ReuseCallerScope_When_PublicationIsRetried(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder { FailNextPublish = true };
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var caller = provider.CreateAsyncScope();
        var db = await factory.CreateDbContextAsync(TestToken);
        db.UseOutbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.SaveChangesAsync(TestToken);
        await db.DisposeAsync();
        var messages = recorder.Messages.ToArray();

        // assert
        Assert.Same(messages[0].Scope, messages[1].Scope);
        Assert.Equal(0, messages[0].Scope.DisposeCount);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_OwnMessagesPerContext_When_FactoryCreatesMultipleContexts(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var first = await factory.CreateDbContextAsync(TestToken);
        var second = await factory.CreateDbContextAsync(TestToken);
        first.UseOutbox(bus);
        second.UseOutbox(bus);
        first.Orders.Add(new Order { Id = 1 });
        second.Orders.Add(new Order { Id = 2 });
        await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        var firstMessage = first.ChangeTracker.Entries<OutboxMessage>().Single();
        var secondEnvelope = second.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;

        // act
        await first.SaveChangesAsync(TestToken);
        var pendingEnvelopeKind = GetEnvelopeKind(secondEnvelope);
        await second.DisposeAsync();
        var messages = recorder.Messages.ToArray();
        var savedOrderIds = await first.Orders.Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal(
            (1L, EntityState.Detached, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), firstMessage.State, pendingEnvelopeKind));
        Assert.Equal([1], savedOrderIds);
        Assert.Same(messages[0].Scope, messages[1].Scope);
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(firstMessage.Entity.Envelope));
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(secondEnvelope));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_PreserveIndependentCommitAndCorrelation_When_HandlerFails(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        var handlerRecorder = new FactoryHandlerRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder,
            handlerRecorder: handlerRecorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        using var incoming = await ((IMessageDispatcher)bus).PreparePublishAsync(
            new StartFactoryWork(), PublishOptions.Default, TestToken);
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = (InMemoryDispatchEndpoint)runtime.GetDispatchEndpoint(
            new Uri(incoming.Envelope.DestinationAddress!));
        await endpoint.Topic!.SendAsync(incoming.Envelope, TestToken);
        await handlerRecorder.Completed.Task.WaitAsync(s_timeout, TestToken);
        var messages = recorder.Messages.ToArray();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = await factory.CreateDbContextAsync(TestToken);
        db.UseOutbox(bus);
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
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
            (1, false, null),
            (messages.Select(m => m.Scope).Append(handlerRecorder.HandlerScope).Distinct().Count(),
                outsideHandler.ConversationId == "factory-conversation", outsideHandler.CausationId));
        Assert.Equal(
            Enumerable.Repeat(("handler", 1), 2),
            messages.Select(message => (message.Scope.Name, message.Scope.DisposeCount)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxDispatch_Should_RejectScheduledMessage_When_OptionsSpecifyScheduledTime(bool send)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var due = DateTimeOffset.UtcNow.AddHours(1);

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            if (send)
            {
                await db.Outbox.SendAsync(
                    new FulfillOrder(1), new SendOptions { ScheduledTime = due }, TestToken);
            }
            else
            {
                await db.Outbox.PublishAsync(
                    new OrderPlaced { OrderId = 1 }, new PublishOptions { ScheduledTime = due }, TestToken);
            }
        });
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal("options", exception.ParamName);
        Assert.Empty(recorder.Messages);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxDispatch_Should_RejectScheduledMessage_When_MiddlewareSuppliesScheduledEnvelope(bool send)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder { ScheduledEnvelopeTime = DateTimeOffset.UtcNow.AddHours(1) };
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        // act
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            if (send)
            {
                await db.Outbox.SendAsync(new FulfillOrder(1), TestToken);
            }
            else
            {
                await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            }
        });
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Single(recorder.Messages);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_ReleaseMessages_When_ChangeTrackerIsCleared(bool asynchronous)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var envelopes = new List<string>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = i }, TestToken);
            envelopes.Add(db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope);
            db.ChangeTracker.Clear();

            if (asynchronous)
            {
                await db.SaveChangesAsync(TestToken);
            }
            else
            {
                db.SaveChanges();
            }
        }

        // assert
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
        Assert.All(envelopes, envelope =>
            Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(envelope)));
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
        db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 1 });

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainTrackedMessage_When_SaveDoesNotAcceptChanges()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox.PublishAsync(
            new OrderPlaced { OrderId = 1 },
            TestToken);

        // act
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();

        // assert
        Assert.Equal(
            (1L, EntityState.Added, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), message.State, GetEnvelopeKind(message.Entity.Envelope)));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ReleaseOnlyClearedMessage_When_SaveDoesNotAcceptChanges()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var discarded = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;
        db.ChangeTracker.Clear();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        var pending = db.ChangeTracker.Entries<OutboxMessage>().Single();

        // act
        await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);

        // assert
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(discarded));
        Assert.Equal(
            (1L, EntityState.Added, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), pending.State, GetEnvelopeKind(pending.Entity.Envelope)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_ReleaseMessageOnNextSave_When_ChangesAreAcceptedAfterCommit(
        bool asynchronous)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var message = db.ChangeTracker.Entries<OutboxMessage>().Single();

        // act
        await using (var transaction = await db.Database.BeginTransactionAsync(TestToken))
        {
            if (asynchronous)
            {
                await db.SaveChangesAsync(acceptAllChangesOnSuccess: false, TestToken);
            }
            else
            {
                db.SaveChanges(acceptAllChangesOnSuccess: false);
            }

            await transaction.CommitAsync(TestToken);
        }

        db.ChangeTracker.AcceptAllChanges();
        var acceptedState = (message.State, GetEnvelopeKind(message.Entity.Envelope));

        if (asynchronous)
        {
            await db.SaveChangesAsync(TestToken);
        }
        else
        {
            db.SaveChanges();
        }

        // assert
        Assert.Equal((EntityState.Unchanged, JsonValueKind.Object), acceptedState);
        Assert.Equal((1L, EntityState.Detached), (await CountOutboxRowsAsync(connectionString), message.State));
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(message.Entity.Envelope));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_NotRestoreMessage_When_TransactionRollsBackAfterAcceptedSave()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var order = db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var envelope = db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;

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
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(envelope));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainMessageForRetry_When_TransactionRollsBackBeforeAcceptance()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
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
            GetEnvelopeKind(message.Entity.Envelope));
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, JsonValueKind.Object), rolledBackState);
        Assert.Equal((1L, 1), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
        Assert.Equal(JsonValueKind.Object, GetEnvelopeKind(message.Entity.Envelope));
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
        Assert.Equal(EntityState.Detached, message.State);
    }

    [Fact]
    public async Task Outbox_Should_Throw_When_DbContextHasNoOutbox()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var db = new UnitOfWorkDbContext(
            new DbContextOptionsBuilder<UnitOfWorkDbContext>().UseTestNpgsql(connectionString).Options);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => db.Outbox);

        // assert
        Assert.Equal(
            "The DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.PostgresDbContextOutboxTests+UnitOfWorkDbContext' "
            + "has no outbox. Register an outbox for it with AddEntityFramework<UnitOfWorkDbContext>(), "
            + "for example by calling UsePostgresOutbox().",
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_RejectUnattachedContext_When_CreatedByFactory(bool pooled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), pooled: pooled, useFactory: true, scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var db = await provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
            .CreateDbContextAsync(TestToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Contains("UseOutbox", exception.Message);
        Assert.Empty(recorder.Messages);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RejectStaleOutbox_When_PooledContextIsLeasedAgain()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), pooled: true, useFactory: true);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var first = await factory.CreateDbContextAsync(TestToken);
        first.UseOutbox(bus);
        var stale = first.Outbox;
        await first.DisposeAsync();
        await using var second = await factory.CreateDbContextAsync(TestToken);
        second.UseOutbox(bus);

        // act
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await stale.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await second.SaveChangesAsync(TestToken);

        // assert
        Assert.Same(first, second);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task UseOutbox_Should_RejectProviderWithoutStaging_When_OnlyImmediatePersistenceIsRegistered()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var immediate = new ImmediateMessageOutbox();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: true, immediateOutbox: immediate);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var db = await provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
            .CreateDbContextAsync(TestToken);
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => db.UseOutbox(bus));

        // assert
        Assert.Contains("staging", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, immediate.PersistCount);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_DiscardPreparedMessage_When_MiddlewareFailsAfterNext(bool useFactory)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder { FailAfterNext = true };
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: useFactory, scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var db = useFactory
            ? await caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
                .CreateDbContextAsync(TestToken)
            : caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        if (useFactory)
        {
            db.UseOutbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());
        }

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        var trackedAfterFailure = db.ChangeTracker.Entries<OutboxMessage>().Count();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(0, trackedAfterFailure);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardPreparedMessage_When_CancelledAfterNext()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var recorder = new ScopeRecorder { CancelAfterNext = cancellation };
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, cancellation.Token));
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_FreezePayload_When_MessageIsChangedBeforeSave()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var message = new OrderPlaced { OrderId = 1 };

        // act
        await db.Outbox.PublishAsync(message, TestToken);
        message.OrderId = 2;
        await db.Outbox.PublishAsync(message, TestToken);
        await db.SaveChangesAsync(TestToken);
        var envelopes = await db.Set<OutboxMessage>().OrderBy(x => x.Id)
            .Select(x => x.Envelope).ToArrayAsync(TestToken);
        var values = envelopes.Select(envelope =>
        {
            var parsed = MessageEnvelopeReader.Parse(System.Text.Encoding.UTF8.GetBytes(envelope));
            return JsonSerializer.Deserialize<OrderPlaced>(parsed.Body.Span)!.OrderId;
        }).ToArray();

        // assert
        Assert.Equal([1, 2], values);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_SnapshotFinalMessage_When_MiddlewareChangesMessageAfterNext()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var sentAt = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);
        var recorder = new ScopeRecorder
        {
            AfterDispatch = async context =>
            {
                await Task.Yield();
                ((OrderPlaced)context.Message!).OrderId = 42;
                context.MessageId = "final-message";
                context.Headers.Set("final-header", "final-value");
                context.SentAt = sentAt;
                context.DeliverBy = sentAt.AddHours(1);
            }
        };
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var message = new OrderPlaced { OrderId = 1 };

        // act
        await db.Outbox.PublishAsync(message, TestToken);
        message.OrderId = 99;
        await db.SaveChangesAsync(TestToken);
        var stored = await db.Set<OutboxMessage>().Select(x => x.Envelope).SingleAsync(TestToken);
        var envelope = MessageEnvelopeReader.Parse(System.Text.Encoding.UTF8.GetBytes(stored));

        // assert
        Assert.Equal(42, JsonSerializer.Deserialize<OrderPlaced>(envelope.Body.Span)!.OrderId);
        Assert.Equal("final-message", envelope.MessageId);
        Assert.Equal("final-value", envelope.Headers!.GetValue("final-header"));
        Assert.Equal((sentAt, sentAt.AddHours(1)), (envelope.SentAt, envelope.DeliverBy));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_RejectNonAtomicSave_When_AutomaticTransactionsAreDisabled()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
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
    public async Task OutboxPublishAsync_Should_SaveMessageAndBusinessChanges_When_UsingExistingSchema()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE outbox_messages (
                id uuid PRIMARY KEY,
                envelope json NOT NULL,
                times_sent integer NOT NULL,
                created_at timestamp with time zone NOT NULL
            );
            CREATE TABLE orders ("Id" integer PRIMARY KEY)
            """;
        await command.ExecuteNonQueryAsync(TestToken);
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await using var caller = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });

        // act
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.SaveChangesAsync(TestToken);
        var orderIds = await db.Orders.Select(order => order.Id).ToArrayAsync(TestToken);
        command.CommandText = "SELECT envelope -> 'body' FROM outbox_messages";
        var body = (string)(await command.ExecuteScalarAsync(TestToken))!;

        // assert
        Assert.Equal([1], orderIds);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(1, JsonSerializer.Deserialize<OrderPlaced>(body)!.OrderId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_RejectSaveDuringPreparation_When_MiddlewareSaves(bool asynchronous)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Orders.Add(new Order { Id = 1 });
        recorder.AfterNext = async () =>
        {
            if (asynchronous)
            {
                await db.SaveChangesAsync(TestToken);
            }
            else
            {
                db.SaveChanges();
            }
        };

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        var orders = await db.Orders.CountAsync(TestToken);

        // assert
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal((0L, 0), (await CountOutboxRowsAsync(connectionString), orders));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RejectReentrantPublish_When_MiddlewarePublishesOnSameContext()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        recorder.AfterNext = () => db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        recorder.AfterNext = null;
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 3 }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_PreserveFirstPublish_When_ConcurrentPublishIsRejected()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        var preparing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        recorder.AfterNext = async () =>
        {
            preparing.SetResult();
            await release.Task.WaitAsync(TestToken);
        };

        // act
        var first = db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await preparing.Task.WaitAsync(TestToken);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken));
        release.SetResult();
        await first;
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseOutbox_Should_AllowSameBusAndRejectDifferentBus_When_AlreadyAttached(bool useFactory)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: useFactory);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var other = provider.CreateAsyncScope();
        await using var db = useFactory
            ? await caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
                .CreateDbContextAsync(TestToken)
            : caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        db.UseOutbox(bus);

        // act
        db.UseOutbox(bus);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            db.UseOutbox(other.ServiceProvider.GetRequiredService<IMessageBus>()));
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal("This DbContext lease is already attached to a different messaging scope.", exception.Message);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Outbox_Should_RejectUnregisteredDestination_When_ContextConnectionChanges(bool afterPublish)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var otherConnection = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: true);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        await using var db = await caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>()
            .CreateDbContextAsync(TestToken);
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        db.UseOutbox(bus);
        if (afterPublish)
        {
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        }

        db.Database.SetConnectionString(otherConnection);

        // act
        var exception = afterPublish
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestToken))
            : await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Contains("not served by its outbox worker", exception.Message);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RemovePartiallyTrackedMessage_When_TrackingCallbackThrows()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var fail = true;
        db.ChangeTracker.Tracked += (_, args) =>
        {
            if (fail && args.Entry.Entity is OutboxMessage)
            {
                fail = false;
                throw new InvalidOperationException("Tracking failed deliberately.");
            }
        };

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        var failedCount = db.ChangeTracker.Entries<OutboxMessage>().Count();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(0, failedCount);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxSendAsync_Should_SaveDirectedMessage_When_PublishedThroughAttachedContext()
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
                var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
                await db.Outbox.SendAsync(new FulfillOrder(42), TestToken);
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
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
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
        var db = caller.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        db.Orders.Add(new Order { Id = 1 });
        db.PendingEvents.Enqueue(new OrderPlaced { OrderId = 1 });

        // act
        if (duringSave)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestToken));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        }

        var orders = await db.Orders.CountAsync(TestToken);

        // assert
        Assert.Equal((0L, 0), (await CountOutboxRowsAsync(connectionString), orders));
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_StageWithPublicProvider_When_CustomStagingIsRegistered(bool save)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var immediate = new ImmediateMessageOutbox();
        await using var provider = await CreateProviderAsync(
            connectionString, new StubOutboxSignal(), useFactory: true,
            immediateOutbox: immediate, customStaging: true);
        await EnsureCreatedAsync(provider);
        await using var caller = provider.CreateAsyncScope();
        var factory = caller.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();

        // act
        await using (var db = await factory.CreateDbContextAsync(TestToken))
        {
            db.UseOutbox(caller.ServiceProvider.GetRequiredService<IMessageBus>());
            db.Orders.Add(new Order { Id = 1 });
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            if (save)
            {
                await db.SaveChangesAsync(TestToken);
            }
        }

        await using var verification = await factory.CreateDbContextAsync(TestToken);
        var orders = await verification.Orders.CountAsync(TestToken);
        var messages = await CountOutboxRowsAsync(connectionString);

        // assert
        Assert.Equal(0, immediate.PersistCount);
        Assert.Equal(save ? (1L, 1) : (0L, 0), (messages, orders));
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        return new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
    }

    private static JsonValueKind GetEnvelopeKind(string envelope)
    {
        using var document = JsonDocument.Parse(envelope);
        return document.RootElement.ValueKind;
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

        // the processor loop is cancelled but not awaited, so let in-flight commits drain
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
        FactoryHandlerRecorder? handlerRecorder = null,
        ImmediateMessageOutbox? immediateOutbox = null,
        bool customStaging = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder ?? new MessageRecorder());
        services.AddLogging();
        services.AddSingleton(signal);

        if (scopeRecorder is not null)
        {
            services.AddSingleton(scopeRecorder);
            services.AddScoped(_ =>
            {
                var marker = new ScopeMarker();
                scopeRecorder.Created.Enqueue(marker);
                return marker;
            });
            services.AddScoped(_ =>
            {
                var marker = new AsyncScopeMarker();
                scopeRecorder.AsyncMarkers.Enqueue(marker);
                return marker;
            });
        }

        if (immediateOutbox is not null)
        {
            services.AddSingleton<IMessageOutbox>(immediateOutbox);
        }

        var builder = services.AddMessageBus();
        if (outboxFirst)
        {
            builder.AddEntityFramework<UnitOfWorkDbContext>(ef =>
            {
                if (immediateOutbox is null)
                {
                    ef.UsePostgresOutbox();
                }
                else
                {
                    ef.UseOutboxCore();
                    if (customStaging)
                    {
                        ef.ConfigureEntityFrameworkServices((_, internalServices) =>
                            internalServices.AddScoped<IDbContextOutboxProvider, CustomOutboxProvider>());
                    }
                }
            });
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
            builder.AddEntityFramework<UnitOfWorkDbContext>(ef =>
            {
                if (immediateOutbox is null)
                {
                    ef.UsePostgresOutbox();
                }
                else
                {
                    ef.UseOutboxCore();
                    if (customStaging)
                    {
                        ef.ConfigureEntityFrameworkServices((_, internalServices) =>
                            internalServices.AddScoped<IDbContextOutboxProvider, CustomOutboxProvider>());
                    }
                }
            });
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
                            var scopeMessages = context.Services.GetRequiredService<ScopeRecorder>();
                            var marker = context.Services.GetRequiredService<ScopeMarker>();
                            scopeMessages.Messages.Enqueue((marker, context.ConversationId, context.CausationId));

                            if (scopeMessages.UseAsyncMarker)
                            {
                                context.Services.GetRequiredService<AsyncScopeMarker>();
                            }

                            if (scopeMessages.FailNextPublish)
                            {
                                scopeMessages.FailNextPublish = false;
                                throw new InvalidOperationException("The publication failed deliberately.");
                            }
                        }

                        await next(context);

                        var recorder = context.Services.GetRequiredService<ScopeRecorder>();
                        if (recorder.FailAfterNext)
                        {
                            recorder.FailAfterNext = false;
                            throw new InvalidOperationException("The publication failed after preparation.");
                        }

                        recorder.CancelAfterNext?.Cancel();
                        if (recorder.AfterNext is { } afterNext)
                        {
                            await afterNext();
                        }

                        if (recorder.AfterDispatch is { } afterDispatch)
                        {
                            await afterDispatch(context);
                        }
                    },
                    "ScopeRecorder"),
                before: "Serialization"));

            if (scopeRecorder.ScheduledEnvelopeTime is { } scheduledTime)
            {
                builder.ConfigureMessageBus(bus => bus.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (_, next) => context =>
                        {
                            context.Envelope = new MessageEnvelope
                            {
                                ScheduledTime = scheduledTime
                            };
                            return next(context);
                        },
                        "ScheduleSerializedEnvelope"),
                    before: "Outbox"));
            }
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestToken);

        if (pooled)
        {
            // Complete table metadata discovery before testing context leases.
            await using var scope = provider.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
        }

        return provider;
    }

    public sealed class UnitOfWorkDbContext : DbContext
    {
        private static int s_createdCount;

        public UnitOfWorkDbContext(DbContextOptions<UnitOfWorkDbContext> options) : base(options)
        {
            Interlocked.Increment(ref s_createdCount);
        }

        public static int CreatedCount => Volatile.Read(ref s_createdCount);

        public bool PublishDomainEvents { get; set; }

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
                await this.Outbox.PublishAsync(domainEvent, cancellationToken);
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

    public sealed class ScopeMarker : IDisposable
    {
        public string Name { get; set; } = string.Empty;

        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class AsyncScopeMarker : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeCount++;
        }
    }

    private sealed class ScopeRecorder
    {
        public ConcurrentQueue<ScopeMarker> Created { get; } = new();

        public ConcurrentQueue<AsyncScopeMarker> AsyncMarkers { get; } = new();

        public ConcurrentQueue<(ScopeMarker Scope, string? ConversationId, string? CausationId)> Messages { get; } = new();

        public bool UseAsyncMarker { get; init; }

        public bool FailNextPublish { get; set; }

        public bool FailAfterNext { get; set; }

        public CancellationTokenSource? CancelAfterNext { get; init; }

        public Func<ValueTask>? AfterNext { get; set; }

        public Func<IDispatchContext, ValueTask>? AfterDispatch { get; init; }

        public DateTimeOffset? ScheduledEnvelopeTime { get; init; }
    }

    private sealed class CustomOutboxProvider : IDbContextOutboxProvider
    {
        public void Validate(DbContext context)
        {
            if (context is not UnitOfWorkDbContext)
            {
                throw new InvalidOperationException("Unsupported context.");
            }
        }

        public IPreparedOutboxMessage Prepare(MessageEnvelope envelope)
        {
            var body = JsonSerializer.Deserialize<OrderPlaced>(envelope.Body.Span);
            return new CustomPreparedMessage(new OutboxMessage(Guid.NewGuid(), JsonSerializer.Serialize(body)));
        }

        public void Stage(DbContext context, IPreparedOutboxMessage message)
            => context.Add(((CustomPreparedMessage)message).Entity);
    }

    private sealed class CustomPreparedMessage(OutboxMessage entity) : IPreparedOutboxMessage
    {
        public OutboxMessage Entity { get; } = entity;

        public void Dispose() { }
    }

    private sealed class ImmediateMessageOutbox : IMessageOutbox
    {
        public int PersistCount { get; private set; }

        public ValueTask PersistAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
        {
            PersistCount++;
            return default;
        }
    }

    public sealed class StartFactoryWork;

    public sealed class FactoryHandlerRecorder
    {
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ScopeMarker? HandlerScope { get; set; }
    }

    private sealed class FactoryHandlerException : Exception;

    public sealed class FactoryPublishingHandler(
        IDbContextFactory<UnitOfWorkDbContext> factory,
        IServiceProvider services,
        IMessageBus bus,
        ScopeMarker marker,
        FactoryHandlerRecorder recorder) : IEventHandler<StartFactoryWork>
    {
        public async ValueTask HandleAsync(StartFactoryWork message, CancellationToken cancellationToken)
        {
            recorder.HandlerScope = marker;
            marker.Name = "handler";
            var managed = (UnitOfWorkDbContext)services
                .GetRequiredKeyedService<DbContextAccessor>(typeof(UnitOfWorkDbContext)).Context;
            managed.Orders.Add(new Order { Id = 3 });
            await managed.SaveChangesAsync(cancellationToken);
            await using var first = await factory.CreateDbContextAsync(cancellationToken);
            await using var second = await factory.CreateDbContextAsync(cancellationToken);
            first.UseOutbox(bus);
            second.UseOutbox(bus);
            first.Orders.Add(new Order { Id = 1 });
            second.Orders.Add(new Order { Id = 2 });
            await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, cancellationToken);
            await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, cancellationToken);
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
