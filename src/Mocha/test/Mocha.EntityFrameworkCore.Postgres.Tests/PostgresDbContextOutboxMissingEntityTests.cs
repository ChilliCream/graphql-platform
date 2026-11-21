#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxMissingEntityTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OutboxPublishAsync_Should_ThrowAddPostgresOutboxError_When_ModelHasNoOutboxEntity()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<NoOutboxEntityDbContext>(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoOutboxEntityDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.Outbox(bus).PublishAsync(new MissingEntityEvent(1), TestToken));

        // assert
        Assert.Equal(
            "The model of the DbContext "
            + $"'{typeof(NoOutboxEntityDbContext).FullName}' has no outbox message entity. "
            + "Call modelBuilder.AddPostgresOutbox() in OnModelCreating.",
            exception.Message);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task OutboxPublishAsync_Should_ThrowTrackingFailure_When_UndoingTheAddAlsoFails()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync<OutboxEntityDbContext>(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxEntityDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        dbContext.ChangeTracker.Tracked += (_, _) => throw new InvalidOperationException("Tracking failed.");
        dbContext.ChangeTracker.StateChanged += (_, _) => throw new InvalidOperationException("Detaching failed.");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dbContext.Outbox(bus).PublishAsync(new MissingEntityEvent(1), TestToken));

        // assert
        Assert.Equal("Tracking failed.", exception.Message);
        Assert.Empty(dbContext.ChangeTracker.Entries<OutboxMessage>());
    }

    private static async Task<ServiceProvider> CreateProviderAsync<TContext>(string connectionString)
        where TContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }

    public sealed class NoOutboxEntityDbContext(DbContextOptions<NoOutboxEntityDbContext> options)
        : DbContext(options);

    public sealed class OutboxEntityDbContext(DbContextOptions<OutboxEntityDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
        }
    }

    public sealed record MissingEntityEvent(int Id);
}
#endif
