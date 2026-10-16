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

    private static string UnboundScopeMessage =>
        $"The DbContext '{typeof(UnitOfWorkDbContext).FullName}' has no application scope for its outbox. "
        + "Register the DbContext before AddEntityFramework and resolve the DbContext itself "
        + "from an application scope. Contexts created directly by IDbContextFactory are not bound to a scope.";

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

    [Fact]
    public async Task OutboxPublishAsync_Should_UseCurrentScope_When_PooledDbContextIsLeasedAgain()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var recorder = new ScopeRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: true,
            scopeRecorder: recorder);
        await EnsureCreatedAsync(provider);
        UnitOfWorkDbContext first;
        UnitOfWorkDbContext second;

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            SetScopeContext(scope.ServiceProvider, "first");
            first = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            await first.SaveChangesAsync(TestToken);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            SetScopeContext(scope.ServiceProvider, "second");
            second = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
            await second.SaveChangesAsync(TestToken);
        }

        // assert
        Assert.Same(first, second);
        Assert.Equal(
            [("first", "first-conversation", "first-message"), ("second", "second-conversation", "second-message")],
            recorder.Messages);
        Assert.Equal(2, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_UseCurrentScope_When_FactoryRegisteredContextIsResolvedFromScope(
        bool pooled)
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
        IDbContextFactory<UnitOfWorkDbContext> scopedFactory;

        // act
        await using (var scope = provider.CreateAsyncScope())
        {
            SetScopeContext(scope.ServiceProvider, "first");
            scopedFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            SetScopeContext(scope.ServiceProvider, "second");
            var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
            await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
            await db.SaveChangesAsync(TestToken);
        }

        // assert
        Assert.Same(factory, scopedFactory);
        Assert.Equal(
            [("first", "first-conversation", "first-message"), ("second", "second-conversation", "second-message")],
            recorder.Messages);
        Assert.Equal(2, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task OutboxPublishAsync_Should_Throw_When_ContextIsCreatedDirectlyByFactory(
        bool pooled,
        bool asynchronous)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true);
        await EnsureCreatedAsync(provider);
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();
        await using var db = asynchronous
            ? await factory.CreateDbContextAsync(TestToken)
            : factory.CreateDbContext();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await db.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken));

        // assert
        Assert.Equal(UnboundScopeMessage, exception.Message);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_Throw_When_FactoryReusesContextFromActiveScope(bool asynchronous)
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
        await using var scope = provider.CreateAsyncScope();
        SetScopeContext(scope.ServiceProvider, "first");
        var first = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await first.SaveChangesAsync(TestToken);
        await first.DisposeAsync();
        var factory = provider.GetRequiredService<IDbContextFactory<UnitOfWorkDbContext>>();

        // act
        await using var second = asynchronous
            ? await factory.CreateDbContextAsync(TestToken)
            : factory.CreateDbContext();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken));

        // assert
        Assert.Same(first, second);
        Assert.Equal(UnboundScopeMessage, exception.Message);
        Assert.Equal([("first", "first-conversation", "first-message")], recorder.Messages);
        Assert.Empty(second.ChangeTracker.Entries<OutboxMessage>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OutboxPublishAsync_Should_OwnMessagesPerContext_When_ContextsAreResolvedFromSeparateScopes(
        bool pooled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            new StubOutboxSignal(),
            pooled: pooled,
            useFactory: true);
        await EnsureCreatedAsync(provider);
        await using var firstScope = provider.CreateAsyncScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await first.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, TestToken);
        await second.Outbox.PublishAsync(new OrderPlaced { OrderId = 2 }, TestToken);
        var firstMessage = first.ChangeTracker.Entries<OutboxMessage>().Single();
        var secondEnvelope = second.ChangeTracker.Entries<OutboxMessage>().Single().Entity.Envelope;

        // act
        await first.SaveChangesAsync(TestToken);
        var pendingEnvelopeKind = secondEnvelope.RootElement.ValueKind;
        secondScope.Dispose();

        // assert
        Assert.Equal(
            (1L, EntityState.Detached, JsonValueKind.Object),
            (await CountOutboxRowsAsync(connectionString), firstMessage.State, pendingEnvelopeKind));
        Assert.Throws<ObjectDisposedException>(() => firstMessage.Entity.Envelope.RootElement.ValueKind);
        Assert.Throws<ObjectDisposedException>(() => secondEnvelope.RootElement.ValueKind);
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

    [Fact]
    public async Task OutboxPublishAsync_Should_DiscardMessage_When_ChangeTrackerIsCleared()
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
        db.ChangeTracker.Clear();
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
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

    private static void SetScopeContext(IServiceProvider services, string name)
    {
        services.GetRequiredService<ScopeMarker>().Name = name;
        services.GetRequiredService<ConsumeContextAccessor>().Context = new ReceiveContext
        {
            MessageId = $"{name}-message",
            ConversationId = $"{name}-conversation"
        };
    }

    private static async Task EnsureCreatedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnitOfWorkDbContext>();
        await db.Database.EnsureCreatedAsync(TestToken);
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
        ScopeRecorder? scopeRecorder = null)
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

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<UnitOfWorkDbContext>(
            ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddInMemory();

        if (scopeRecorder is not null)
        {
            builder.ConfigureMessageBus(bus => bus.UseDispatch(
                new DispatchMiddlewareConfiguration(
                    static (_, next) => async context =>
                    {
                        var scopeMessages = context.Services.GetRequiredService<ScopeRecorder>();
                        var marker = context.Services.GetRequiredService<ScopeMarker>();
                        scopeMessages.Messages.Enqueue((marker.Name, context.ConversationId, context.CausationId));
                        await next(context);
                    },
                    "ScopeRecorder"),
                before: "Outbox"));
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

        public DbSet<Order> Orders => Set<Order>();

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            if (PublishDomainEvents)
            {
                var orders = ChangeTracker.Entries<Order>().Where(e => e.State == EntityState.Added).ToArray();
                foreach (var entry in orders)
                {
                    await this.Outbox.PublishAsync(
                        new OrderPlaced { OrderId = entry.Entity.Id },
                        cancellationToken);
                }
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
            await eventData.Context!.Outbox.PublishAsync(new OrderPlaced { OrderId = 1 }, cancellationToken);
            return result;
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    private sealed class ScopeMarker
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ScopeRecorder
    {
        public ConcurrentQueue<(string Scope, string? ConversationId, string? CausationId)> Messages { get; } = new();
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
