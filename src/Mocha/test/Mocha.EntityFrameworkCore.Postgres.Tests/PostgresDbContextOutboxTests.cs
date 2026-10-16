using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
        Assert.Throws<ObjectDisposedException>(() => message.Entity.Envelope.RootElement.ValueKind);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowOnSave_When_SaveChangesIsSynchronous()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateProviderAsync(connectionString, signal);
        await EnsureCreatedAsync(provider);
        var setCountBefore = signal.SetCallCount;
        JsonDocument envelope;

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
        Assert.Throws<ObjectDisposedException>(() => envelope.RootElement.ValueKind);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainMessageForRetry_When_SaveFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
            message.Entity.Envelope.RootElement.ValueKind);
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true);
        await EnsureCreatedAsync(provider);
        UnitOfWorkDbContext leased;
        JsonDocument envelope;
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
        Assert.Throws<ObjectDisposedException>(() => envelope.RootElement.ValueKind);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task OutboxPublishAsync_Should_ReuseOwnedScope_When_ContextPublishesMultipleMessages(
        bool pooled,
        bool useFactory)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        Assert.NotSame(callerMarker, messages[0].Scope);
        Assert.Equal(
            (string.Empty, 1, 0, 1),
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
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            outboxFirst: outboxFirst);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = asynchronous
            ? await factory.CreateDbContextAsync(TestToken)
            : factory.CreateDbContext();
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
    public async Task OutboxPublishAsync_Should_CreateNewScope_When_FactoryReusesPooledContext(bool asynchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var first = factory.CreateDbContext();
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

        await using var second = factory.CreateDbContext();
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        await second.SaveChangesAsync(TestToken);
        var messages = recorder.Messages.ToArray();

        // assert
        Assert.Same(first, second);
        Assert.NotSame(messages[0].Scope, messages[1].Scope);
        Assert.Equal((1, 0), (messages[0].Scope.DisposeCount, messages[1].Scope.DisposeCount));
        Assert.Throws<ObjectDisposedException>(() => discarded.RootElement.ValueKind);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposeAsync_Should_DisposeAsyncScopedServices_When_FactoryContextIsReleased(bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new ScopeRecorder { UseAsyncMarker = true };
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var db = await factory.CreateDbContextAsync(TestToken);
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        var marker = recorder.AsyncMarkers.Single();

        // act
        await db.DisposeAsync();

        // assert
        Assert.Equal(1, marker.DisposeCount);
        Assert.Equal(1, recorder.Created.Single().DisposeCount);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_Throw_When_OwningContextIsDisposed(bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
    public async Task OutboxPublishAsync_Should_ReuseAndDisposeScope_When_PublicationIsRetried(bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new ScopeRecorder { FailNextPublish = true };
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        var db = await factory.CreateDbContextAsync(TestToken);

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));
        await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await db.SaveChangesAsync(TestToken);
        await db.DisposeAsync();
        var messages = recorder.Messages.ToArray();

        // assert
        Assert.Same(messages[0].Scope, messages[1].Scope);
        Assert.Equal(1, messages[0].Scope.DisposeCount);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_OwnMessagesPerContext_When_FactoryCreatesMultipleContexts(bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var first = await factory.CreateDbContextAsync(TestToken);
        var second = await factory.CreateDbContextAsync(TestToken);
        first.Orders.Add(new Order { Id = 1 });
        second.Orders.Add(new Order { Id = 2 });
        await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        var firstMessage = first.ChangeTracker.Entries<OutboxMessage>().Single();
        var secondEnvelope = second.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;

        // act
        await first.SaveChangesAsync(TestToken);
        var pendingEnvelopeKind = secondEnvelope.RootElement.ValueKind;
        await second.DisposeAsync();
        var messages = recorder.Messages.ToArray();
        var savedOrderIds = await first.Orders.Select(o => o.Id).ToArrayAsync(TestToken);

        // assert
        Assert.Equal(
            (1L, EntityState.Detached, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), firstMessage.State, pendingEnvelopeKind));
        Assert.Equal([1], savedOrderIds);
        Assert.NotSame(messages[0].Scope, messages[1].Scope);
        Assert.Throws<ObjectDisposedException>(() => firstMessage.Entity.Envelope.RootElement.ValueKind);
        Assert.Throws<ObjectDisposedException>(() => secondEnvelope.RootElement.ValueKind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_PreserveIndependentCommitAndCorrelation_When_HandlerFails(bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        await bus.PublishAsync(new StartFactoryWork(), TestToken);
        await handlerRecorder.Completed.Task.WaitAsync(s_timeout, TestToken);
        var messages = recorder.Messages.ToArray();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = await factory.CreateDbContextAsync(TestToken);
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
            (3, false, null),
            (messages.Select(m => m.Scope).Append(handlerRecorder.HandlerScope).Distinct().Count(),
                outsideHandler.ConversationId == "factory-conversation", outsideHandler.CausationId));
        Assert.Equal(
            Enumerable.Repeat((string.Empty, 1), 2),
            messages.Select(message => (message.Scope.Name, message.Scope.DisposeCount)));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_Throw_When_MessageIsScheduled()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var options = new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(1) };

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, options, TestToken));

        // assert
        Assert.Equal("options", exception.ParamName);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_ReleaseMessages_When_ChangeTrackerIsCleared(bool asynchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString, new StubOutboxSignal());
        await EnsureCreatedAsync(provider);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var envelopes = new List<JsonDocument>();

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
            Assert.Throws<ObjectDisposedException>(() => envelope.RootElement.ValueKind));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowWithSave_When_PublishedFromSavingChangesInterceptor()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
            (await CountOutboxRowsAsync(connectionString), message.State, message.Entity.Envelope.RootElement.ValueKind));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ReleaseOnlyClearedMessage_When_SaveDoesNotAcceptChanges()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        Assert.Throws<ObjectDisposedException>(() => discarded.RootElement.ValueKind);
        Assert.Equal(
            (1L, EntityState.Added, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), pending.State, pending.Entity.Envelope.RootElement.ValueKind));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_ReleaseMessageOnNextSave_When_ChangesAreAcceptedAfterCommit(
        bool asynchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var acceptedState = (message.State, message.Entity.Envelope.RootElement.ValueKind);

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
        Assert.Throws<ObjectDisposedException>(() => message.Entity.Envelope.RootElement.ValueKind);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_NotRestoreMessage_When_TransactionRollsBackAfterAcceptedSave()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        Assert.Throws<ObjectDisposedException>(() => envelope.RootElement.ValueKind);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_RetainMessageForRetry_When_TransactionRollsBackBeforeAcceptance()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
            message.Entity.Envelope.RootElement.ValueKind);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal((0L, EntityState.Added, JsonValueKind.Object), rolledBackState);
        Assert.Equal((1L, 1), (await CountOutboxRowsAsync(connectionString), await db.Orders.CountAsync(TestToken)));
        Assert.Throws<ObjectDisposedException>(() => message.Entity.Envelope.RootElement.ValueKind);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_StageDomainEventOnce_When_SaveIsRetried(bool useInterceptor)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
        var connectionString = await fixture.CreateDatabaseAsync();
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
        FactoryHandlerRecorder? handlerRecorder = null)
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
                            context.SkipOutbox();
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
                    },
                    "ScopeRecorder"),
                before: "Serialization"));
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
            first.Orders.Add(new Order { Id = 1 });
            second.Orders.Add(new Order { Id = 2 });
            await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, cancellationToken);
            await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, cancellationToken);
            await first.SaveChangesAsync(cancellationToken);
            throw new FactoryHandlerException();
        }
    }

    public sealed class OrderPlaced
    {
        public required int OrderId { get; init; }
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
