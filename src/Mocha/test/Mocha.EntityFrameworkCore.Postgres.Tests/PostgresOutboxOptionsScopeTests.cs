#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxOptionsScopeTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static readonly DateTimeOffset s_now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChangesAsync_Should_SignalOutbox_When_ScopedFactoryOptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedFactory,
            signal);
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedFactory);
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(2, await CountOrdersAsync(connectionString));
        Assert.Equal(2, signal.SetCallCount);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_SignalOutbox_When_ConstructorOptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedOptions,
            signal);
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedOptions);
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(2, await CountOrdersAsync(connectionString));
        Assert.Equal(2, signal.SetCallCount);
    }

    [Fact]
    public async Task CommitAsync_Should_SignalOutbox_When_OptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new StubOutboxSignal();
        await using var provider = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedFactory,
            signal);
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedFactory);
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
        db.Orders.Add(new Order { Id = 2 });
        await db.SaveChangesAsync(TestToken);

        // act
        await transaction.CommitAsync(TestToken);

        // assert
        Assert.Equal(2, await CountOrdersAsync(connectionString));
        Assert.Equal(2, signal.SetCallCount);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_NotifyScheduler_When_OptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new RecordingSchedulerSignal();
        await using var provider = await CreateSchedulingProviderAsync(connectionString, signal);
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedFactory);
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(2, await CountOrdersAsync(connectionString));
        Assert.Equal([s_now, s_now], signal.Notifications);
    }

    [Fact]
    public async Task CommitAsync_Should_NotifyScheduler_When_OptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        var signal = new RecordingSchedulerSignal();
        await using var provider = await CreateSchedulingProviderAsync(connectionString, signal);
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedFactory);
        await using var transaction = await db.Database.BeginTransactionAsync(TestToken);
        db.Orders.Add(new Order { Id = 2 });
        await db.SaveChangesAsync(TestToken);

        // act
        await transaction.CommitAsync(TestToken);

        // assert
        Assert.Equal(2, await CountOrdersAsync(connectionString));
        Assert.Equal([s_now, s_now], signal.Notifications);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_StageSaveAndDeliver_When_OptionsScopeIsDisposed()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            ContextRegistration.ScopedFactory,
            static _ => { },
            static ef => ef.UsePostgresOutbox());
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<MessageRecorder>();
        await using var db = await CreateContextOutlivingScopeAsync(provider, ContextRegistration.ScopedFactory);
        await using var caller = provider.CreateAsyncScope();
        var bus = caller.ServiceProvider.GetRequiredService<IMessageBus>();
        db.Orders.Add(new Order { Id = 2 });

        // act
        await db.Outbox(bus).PublishAsync(new OrderPlaced(2), TestToken);
        await db.SaveChangesAsync(TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(new OrderPlaced(2), delivered);
        Assert.Equal(2, await CountOrdersAsync(connectionString));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_SignalOwnApplication_When_ApplicationsShareEntityFrameworkServices()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        StubOutboxSignal[] signals = [new(), new(), new()];
        await using var first = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedOptions,
            signals[0]);
        await using var second = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedOptions,
            signals[1]);
        await using var third = await CreateOutboxProviderAsync(
            connectionString,
            ContextRegistration.ScopedOptions,
            signals[2]);
        ServiceProvider[] providers = [first, second, third];
        var modelSources = new HashSet<IModelSource>(ReferenceEqualityComparer.Instance);

        // act
        for (var i = 0; i < providers.Length; i++)
        {
            await using var scope = providers[i].CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ScopeDbContext>();
            db.Orders.Add(new Order { Id = i + 1 });
            await db.SaveChangesAsync(TestToken);
            modelSources.Add(db.GetService<IModelSource>());
        }

        // assert
        Assert.Single(modelSources);
        Assert.Equal([true, true, true], signals.Select(s => s.WasSet));
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<ScopeDbContext>().UseTestNpgsql(connectionString).Options;
        await using var db = new ScopeDbContext(options);
        await db.Database.EnsureCreatedAsync(TestToken);
        return connectionString;
    }

    private static async Task<ScopeDbContext> CreateContextOutlivingScopeAsync(
        IServiceProvider provider,
        ContextRegistration registration)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = registration == ContextRegistration.ScopedFactory
            ? await scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<ScopeDbContext>>()
                .CreateDbContextAsync(TestToken)
            : new ScopeDbContext(scope.ServiceProvider.GetRequiredService<DbContextOptions<ScopeDbContext>>());
        db.Orders.Add(new Order { Id = 1 });
        await db.SaveChangesAsync(TestToken);
        return db;
    }

    private static async Task<long> CountOrdersAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM orders";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static Task<ServiceProvider> CreateOutboxProviderAsync(
        string connectionString,
        ContextRegistration registration,
        IOutboxSignal signal)
        => CreateProviderAsync(
            connectionString,
            registration,
            services => services.AddSingleton(signal),
            static ef => ef.UsePostgresOutbox());

    private static Task<ServiceProvider> CreateSchedulingProviderAsync(
        string connectionString,
        ISchedulerSignal signal)
        => CreateProviderAsync(
            connectionString,
            ContextRegistration.ScopedFactory,
            services =>
            {
                services.AddSingleton(signal);
                services.AddSingleton<TimeProvider>(new FakeTimeProvider(s_now));
            },
            static ef => ef.UsePostgresScheduling());

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        ContextRegistration registration,
        Action<IServiceCollection> configureServices,
        Action<IEntityFrameworkCoreBuilder> configureEntityFramework)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MessageRecorder>();
        configureServices(services);

        if (registration == ContextRegistration.ScopedFactory)
        {
            services.AddDbContextFactory<ScopeDbContext>(
                options => options.UseTestNpgsql(connectionString),
                ServiceLifetime.Scoped);
        }
        else
        {
            services.AddDbContext<ScopeDbContext>(options => options.UseTestNpgsql(connectionString));
        }

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<ScopeDbContext>(configureEntityFramework);
        builder.AddEventHandler<OrderPlacedHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

    private enum ContextRegistration
    {
        ScopedFactory,
        ScopedOptions
    }

    public sealed class ScopeDbContext(DbContextOptions<ScopeDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.AddPostgresScheduledMessages();
            modelBuilder.Entity<Order>().ToTable("orders").Property(order => order.Id).ValueGeneratedNever();
        }
    }

    public sealed class Order
    {
        public int Id { get; set; }
    }

    public sealed record OrderPlaced(int OrderId);

    public sealed class MessageRecorder
    {
        public TaskCompletionSource<OrderPlaced> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class OrderPlacedHandler(MessageRecorder recorder) : IEventHandler<OrderPlaced>
    {
        public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message);
            return default;
        }
    }

    private sealed class RecordingSchedulerSignal : ISchedulerSignal
    {
        public ConcurrentQueue<DateTimeOffset> Notifications { get; } = new();

        public void Notify(DateTimeOffset scheduledTime) => Notifications.Enqueue(scheduledTime);

        public Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
            => Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
#endif
