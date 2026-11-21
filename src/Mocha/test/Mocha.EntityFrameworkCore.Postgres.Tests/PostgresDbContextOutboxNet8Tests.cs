#if !NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxNet8Tests
{
    [Fact]
    public async Task Outbox_Should_ThrowNotSupported_When_EntityFrameworkCoreIsOlderThan9()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<Net8DbContext>(options => options.UseTestNpgsql("Host=localhost;Database=unused"));
        services.AddMessageBus()
            .AddEntityFramework<Net8DbContext>(ef => ef.UsePostgresOutbox())
            .AddInMemory();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Net8DbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var exception = Assert.Throws<NotSupportedException>(() => db.Outbox(bus));

        // assert
        Assert.Equal(
            "The DbContext outbox requires Entity Framework Core 9 or later. Target .NET 9 or later to use it.",
            exception.Message);
    }

    public sealed class Net8DbContext(DbContextOptions<Net8DbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddPostgresOutbox();
    }
}
#endif
