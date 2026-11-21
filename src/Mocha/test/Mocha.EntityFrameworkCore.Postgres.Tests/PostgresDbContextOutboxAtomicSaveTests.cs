#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresDbContextOutboxAtomicSaveTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveChangesAsync_Should_WriteNoMessage_When_BusinessInsertFails(bool afterOutboxInsert)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        await using var provider = await CreateProviderAsync(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<AtomicSaveDbContext>();
        // EF Core writes the invoices table before the outbox table and the shipments table after it.
        db.Add<object>(afterOutboxInsert ? new Shipment { Id = 1 } : new Invoice { Id = 1 });
        await db.Outbox(bus).PublishAsync(new InvoiceCreated(1), TestToken);

        // act
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestToken));

        // assert
        Assert.Equal(0, await CountOutboxRowsAsync(connectionString));
        Assert.Equal(EntityState.Added, db.ChangeTracker.Entries<OutboxMessage>().Single().State);
    }

    private static async Task<long> CountOutboxRowsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM outbox_messages";
        return (long)(await command.ExecuteScalarAsync(TestToken))!;
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AtomicSaveDbContext>(options => options.UseTestNpgsql(connectionString));
        services.AddMessageBus()
            .AddEntityFramework<AtomicSaveDbContext>(ef => ef.UsePostgresOutbox())
            .AddInMemory();
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtomicSaveDbContext>();
        await db.Database.EnsureCreatedAsync(TestToken);
        db.Add(new Invoice { Id = 1 });
        db.Add(new Shipment { Id = 1 });
        await db.SaveChangesAsync(TestToken);

        return provider;
    }

    public sealed class AtomicSaveDbContext(DbContextOptions<AtomicSaveDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresOutbox();
            modelBuilder.Entity<Invoice>().ToTable("invoices").Property(invoice => invoice.Id).ValueGeneratedNever();
            modelBuilder.Entity<Shipment>().ToTable("shipments").Property(shipment => shipment.Id).ValueGeneratedNever();
        }
    }

    public sealed class Invoice
    {
        public int Id { get; set; }
    }

    public sealed class Shipment
    {
        public int Id { get; set; }
    }

    public sealed record InvoiceCreated(int InvoiceId);
}
#endif
