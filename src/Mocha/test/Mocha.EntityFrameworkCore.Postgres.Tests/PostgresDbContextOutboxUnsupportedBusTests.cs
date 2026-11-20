#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxUnsupportedBusTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Outbox_Should_Throw_When_BusIsNotTheMessageBusFromDependencyInjection()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UnsupportedBusDbContext>();
        var bus = new ForwardingMessageBus(scope.ServiceProvider.GetRequiredService<IMessageBus>());

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => dbContext.Outbox(bus));

        // assert
        Assert.Equal(
            "The message bus 'ForwardingMessageBus' does not support preparing messages for a DbContext outbox. "
            + "Pass the IMessageBus resolved from dependency injection.",
            exception.Message);
        Assert.Empty(dbContext.ChangeTracker.Entries<OutboxMessage>());
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<UnsupportedBusDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<UnsupportedBusDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

    public sealed class UnsupportedBusDbContext(DbContextOptions<UnsupportedBusDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    private sealed class ForwardingMessageBus(IMessageBus inner) : IMessageBus
    {
        public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken) where T : notnull
            => inner.PublishAsync(message, cancellationToken);

        public ValueTask PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken)
            where T : notnull
            => inner.PublishAsync(message, options, cancellationToken);

        public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken) where T : notnull
            => inner.SendAsync(message, cancellationToken);

        public ValueTask SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken)
            where T : notnull
            => inner.SendAsync(message, options, cancellationToken);

        public ValueTask<TResponse> RequestAsync<TResponse>(
            IEventRequest<TResponse> message,
            CancellationToken cancellationToken)
            => inner.RequestAsync(message, cancellationToken);

        public ValueTask<TResponse> RequestAsync<TResponse>(
            IEventRequest<TResponse> message,
            SendOptions options,
            CancellationToken cancellationToken)
            => inner.RequestAsync(message, options, cancellationToken);

        public ValueTask RequestAsync(object message, CancellationToken cancellationToken)
            => inner.RequestAsync(message, cancellationToken);

        public ValueTask RequestAsync(object message, SendOptions options, CancellationToken cancellationToken)
            => inner.RequestAsync(message, options, cancellationToken);

        public ValueTask ReplyAsync<TResponse>(
            TResponse response,
            ReplyOptions options,
            CancellationToken cancellationToken)
            where TResponse : notnull
            => inner.ReplyAsync(response, options, cancellationToken);

        public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            CancellationToken cancellationToken)
            where T : notnull
            => inner.SchedulePublishAsync(message, scheduledTime, cancellationToken);

        public ValueTask<SchedulingResult> SchedulePublishAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            PublishOptions options,
            CancellationToken cancellationToken)
            where T : notnull
            => inner.SchedulePublishAsync(message, scheduledTime, options, cancellationToken);

        public ValueTask<SchedulingResult> ScheduleSendAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            CancellationToken cancellationToken)
            where T : notnull
            => inner.ScheduleSendAsync(message, scheduledTime, cancellationToken);

        public ValueTask<SchedulingResult> ScheduleSendAsync<T>(
            T message,
            DateTimeOffset scheduledTime,
            SendOptions options,
            CancellationToken cancellationToken)
            where T : notnull
            => inner.ScheduleSendAsync(message, scheduledTime, options, cancellationToken);

        public ValueTask<bool> CancelScheduledMessageAsync(string token, CancellationToken cancellationToken)
            => inner.CancelScheduledMessageAsync(token, cancellationToken);
    }
}
#endif
