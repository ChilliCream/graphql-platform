#if NET9_0_OR_GREATER
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxSkipTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly DateTimeOffset s_scheduledTime = new(2050, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task OutboxDispatch_Should_Throw_When_UnanchoredMiddlewareSkipsOutbox(bool send, bool scheduled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            bus => bus.UseDispatch(CreateSkipOutboxMiddleware()));
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<SkipDbContext>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await DispatchAsync(db.Outbox(bus), send, scheduled));
        var staged = db.ChangeTracker.Entries<OutboxMessage>().Count();
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(
            "SkipOutbox() cannot be used for a message published or sent through dbContext.Outbox(bus), "
            + "because the message would be sent before the DbContext transaction commits.",
            exception.Message);
        Assert.Equal(0, staged);
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboxPublishAsync_Should_StageMessage_When_MiddlewareSetsSkipFlagDirectly(bool scheduled)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var transport = new TransportRecorder();
        await using var provider = await CreateProviderAsync(
            connectionString,
            bus =>
            {
                bus.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        static (_, next) => context =>
                        {
                            context.Features.GetOrSet<OutboxMiddlewareFeature>().SkipOutbox = true;
                            return next(context);
                        },
                        "SetSkipOutboxFlag"));
                bus.UseDispatch(transport.Middleware, after: OutboxDispatchMiddlewares.Outbox.Key);
            });
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<SkipDbContext>();

        // act
        await DispatchAsync(db.Outbox(bus), send: false, scheduled);
        await bus.PublishAsync(new SkipEvent("plain"), TestToken);
        var staged = db.ChangeTracker.Entries<OutboxMessage>().Count();
        var sentBeforeSave = transport.Payloads.ToArray();
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, staged);
        Assert.Equal(["plain"], sentBeforeSave);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_StageScheduledMessage_When_SkipMiddlewareIsAnchoredBeforeOutbox()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(
            connectionString,
            bus => bus.UseDispatch(CreateSkipOutboxMiddleware(), before: OutboxDispatchMiddlewares.Outbox.Key));
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<SkipDbContext>();

        // act
        await DispatchAsync(db.Outbox(bus), send: false, scheduled: true);
        var staged = db.ChangeTracker.Entries<OutboxMessage>().Count();
        await db.SaveChangesAsync(TestToken);

        // assert
        Assert.Equal(1, staged);
        Assert.Equal(1, await CountOutboxRowsAsync(connectionString));
    }

    private static DispatchMiddlewareConfiguration CreateSkipOutboxMiddleware()
        => new(
            static (_, next) => context =>
            {
                context.SkipOutbox();
                return next(context);
            },
            "SkipOutbox");

    private static ValueTask DispatchAsync(DbContextOutbox outbox, bool send, bool scheduled)
    {
        DateTimeOffset? scheduledTime = scheduled ? s_scheduledTime : null;

        return send
            ? outbox.SendAsync(new SkipCommand("staged"), new SendOptions { ScheduledTime = scheduledTime }, TestToken)
            : outbox.PublishAsync(
                new SkipEvent("staged"),
                new PublishOptions { ScheduledTime = scheduledTime },
                TestToken);
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

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        Action<IMessageBusBuilder> configureBus)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SkipDbContext>(options => options.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<SkipDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEventHandler<SkipEventHandler>();
        builder.AddRequestHandler<SkipCommandHandler>();
        builder.ConfigureMessageBus(configureBus);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SkipDbContext>().Database.EnsureCreatedAsync(TestToken);

        return provider;
    }

    public sealed class SkipDbContext(DbContextOptions<SkipDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    public sealed record SkipEvent(string Payload);

    public sealed record SkipCommand(string Payload) : IEventRequest;

    public sealed class SkipEventHandler : IEventHandler<SkipEvent>
    {
        public ValueTask HandleAsync(SkipEvent message, CancellationToken cancellationToken) => default;
    }

    public sealed class SkipCommandHandler : IEventRequestHandler<SkipCommand>
    {
        public ValueTask HandleAsync(SkipCommand message, CancellationToken cancellationToken) => default;
    }

    private sealed class TransportRecorder
    {
        public ConcurrentQueue<string> Payloads { get; } = new();

        public DispatchMiddlewareConfiguration Middleware
            => new(
                (_, next) => context =>
                {
                    if (context.Message is SkipEvent message)
                    {
                        Payloads.Enqueue(message.Payload);
                    }

                    return next(context);
                },
                "TransportRecorder");
    }
}
#endif
