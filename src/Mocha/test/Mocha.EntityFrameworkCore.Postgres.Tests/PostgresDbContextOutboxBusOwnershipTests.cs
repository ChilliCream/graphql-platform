#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxBusOwnershipTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static readonly string s_otherApplicationMessage =
        "The message bus was not resolved from the service provider that registers the outbox of the "
        + $"DbContext '{typeof(OwnershipDbContext).FullName}'. Pass the IMessageBus resolved from that "
        + "service provider.";

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_Throw_When_BusComesFromApplicationWithoutOutbox(bool scheduled)
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateOutboxProviderAsync(connectionString);
        var dispatches = new DispatchCounter();
        var otherServices = new ServiceCollection();
        otherServices.AddLogging();
        var otherBuilder = otherServices.AddMessageBus();
        otherBuilder.ConfigureMessageBus(bus => bus.UseDispatch(dispatches.Middleware));
        otherBuilder.AddInMemory();
        await using var other = await StartAsync(otherServices);
        await using var scope = provider.CreateAsyncScope();
        await using var otherScope = other.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OwnershipDbContext>();
        var otherBus = otherScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = new PublishOptions
        {
            ScheduledTime = scheduled ? new DateTimeOffset(2050, 1, 1, 0, 0, 0, TimeSpan.Zero) : null
        };

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await db.Outbox(otherBus).PublishAsync(new OwnershipEvent(1), options, TestToken));

        // assert
        Assert.Equal(s_otherApplicationMessage, exception.Message);
        Assert.Equal(0, dispatches.Count);
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());
    }

    [Fact]
    public async Task Outbox_Should_AcceptOnlyOwnBus_When_HostsShareEntityFrameworkServices()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var first = await CreateOutboxProviderAsync(connectionString);
        await using var second = await CreateOutboxProviderAsync(connectionString);
        await using var firstScope = first.CreateAsyncScope();
        await using var secondScope = second.CreateAsyncScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<OwnershipDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<OwnershipDbContext>();
        var firstBus = firstScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var secondBus = secondScope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        string?[] rejections =
        [
            Record.Exception(() => firstDb.Outbox(secondBus))?.Message,
            Record.Exception(() => secondDb.Outbox(firstBus))?.Message
        ];
        await firstDb.Outbox(firstBus).PublishAsync(new OwnershipEvent(1), TestToken);
        await secondDb.Outbox(secondBus).PublishAsync(new OwnershipEvent(2), TestToken);
        int[] staged =
        [
            firstDb.ChangeTracker.Entries<OutboxMessage>().Count(),
            secondDb.ChangeTracker.Entries<OutboxMessage>().Count()
        ];

        // assert
        Assert.Same(firstDb.GetService<IModelSource>(), secondDb.GetService<IModelSource>());
        Assert.Equal([s_otherApplicationMessage, s_otherApplicationMessage], rejections);
        Assert.Equal([1, 1], staged);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterSave_When_BusAndContextComeFromDifferentScopes()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateOutboxProviderAsync(connectionString);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<DeliveryRecorder>();
        await using var busScope = provider.CreateAsyncScope();
        await using var contextScope = provider.CreateAsyncScope();
        var bus = busScope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = contextScope.ServiceProvider.GetRequiredService<OwnershipDbContext>();

        // act
        await db.Outbox(bus).PublishAsync(new OwnershipEvent(1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(new OwnershipEvent(1), delivered);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_DeliverAfterSave_When_ContextComesFromFactory()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateOutboxProviderAsync(connectionString, useFactory: true);
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var recorder = provider.GetRequiredService<DeliveryRecorder>();
        await using var busScope = provider.CreateAsyncScope();
        var bus = busScope.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var db = await provider.GetRequiredService<IDbContextFactory<OwnershipDbContext>>()
            .CreateDbContextAsync(TestToken);

        // act
        await db.Outbox(bus).PublishAsync(new OwnershipEvent(1), TestToken);
        await db.SaveChangesAsync(TestToken);
        var delivered = await recorder.Delivered.Task.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(new OwnershipEvent(1), delivered);
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_WriteRowOnSave_When_ContextIsConstructedWithApplicationOptions()
    {
        // arrange
        var connectionString = await CreateDatabaseAsync();
        await using var provider = await CreateOutboxProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var db = new OwnershipDbContext(
            scope.ServiceProvider.GetRequiredService<DbContextOptions<OwnershipDbContext>>());

        // act
        await db.Outbox(bus).PublishAsync(new OwnershipEvent(1), TestToken);
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var options = new DbContextOptionsBuilder<OwnershipDbContext>().UseTestNpgsql(connectionString).Options;
        await using var db = new OwnershipDbContext(options);
        await db.Database.EnsureCreatedAsync(TestToken);
        return connectionString;
    }

    private static async Task<long> CountOutboxRowsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static Task<ServiceProvider> CreateOutboxProviderAsync(string connectionString, bool useFactory = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DeliveryRecorder>();

        if (useFactory)
        {
            services.AddDbContextFactory<OwnershipDbContext>(options => options.UseTestNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<OwnershipDbContext>(options => options.UseTestNpgsql(connectionString));
        }

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<OwnershipDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<OwnershipEventHandler>();
        builder.AddInMemory();

        return StartAsync(services);
    }

    private static async Task<ServiceProvider> StartAsync(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

    public sealed class OwnershipDbContext(DbContextOptions<OwnershipDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    public sealed record OwnershipEvent(int Id);

    public sealed class DeliveryRecorder
    {
        public TaskCompletionSource<OwnershipEvent> Delivered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class OwnershipEventHandler(DeliveryRecorder recorder) : IEventHandler<OwnershipEvent>
    {
        public ValueTask HandleAsync(OwnershipEvent message, CancellationToken cancellationToken)
        {
            recorder.Delivered.TrySetResult(message);
            return default;
        }
    }

    private sealed class DispatchCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public DispatchMiddlewareConfiguration Middleware
            => new(
                (_, next) => context =>
                {
                    Interlocked.Increment(ref _count);
                    return next(context);
                },
                "DispatchCounter");
    }
}
#endif
