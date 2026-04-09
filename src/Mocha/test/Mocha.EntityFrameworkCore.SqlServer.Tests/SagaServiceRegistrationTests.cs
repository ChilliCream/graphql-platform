using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;
using Mocha.Sagas;
using Mocha.Sagas.EfCore;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests;

public sealed class SagaServiceRegistrationTests
{
    private const string ConnectionString = "Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True";

    [Fact]
    public void Create_Should_ReturnSagaStore_When_ValidDependencies()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlServer(ConnectionString).Options;

        using var context = new TestDbContext(options);
        var queries = SqlServerSagaStoreQueries.From(new SagaStateTableInfo());

        // Act
        using var store = new SqlServerSagaStore(context, queries, TimeProvider.System);

        // Assert
        Assert.IsAssignableFrom<ISagaStore>(store);
        Assert.IsType<SqlServerSagaStore>(store);
    }

    [Fact]
    public void Queries_Should_BeConfigured_When_CreatedFromDefaultTableInfo()
    {
        // Arrange & Act
        var queries = SqlServerSagaStoreQueries.From(new SagaStateTableInfo());

        // Assert - verify all query strings are populated (non-null, non-empty).
        Assert.False(string.IsNullOrWhiteSpace(queries.SelectState));
        Assert.False(string.IsNullOrWhiteSpace(queries.SelectVersion));
        Assert.False(string.IsNullOrWhiteSpace(queries.InsertState));
        Assert.False(string.IsNullOrWhiteSpace(queries.UpdateState));
        Assert.False(string.IsNullOrWhiteSpace(queries.DeleteState));
    }

    [Fact]
    public async Task AddSqlServerSagas_Should_RegisterScopedSagaStore_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetService<ISagaStore>();

        // Assert
        Assert.NotNull(store);
        Assert.IsType<SqlServerSagaStore>(store);
    }

    [Fact]
    public async Task AddSqlServerSagas_Should_ConfigureQueriesFromModel_When_DefaultTableNames()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<SqlServerSagaStoreOptions>>();
        var contextName = typeof(TestDbContext).FullName!;
        var options = optionsMonitor.Get(contextName);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.SelectState));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.SelectVersion));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.InsertState));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.UpdateState));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.DeleteState));
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<TestDbContext>(o => o.UseSqlServer(ConnectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.AddSqlServerSagas());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        // Build the runtime so that all singleton factories resolve
        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }
}
