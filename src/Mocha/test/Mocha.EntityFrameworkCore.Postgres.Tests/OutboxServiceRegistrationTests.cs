using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class OutboxServiceRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=test";

    [Fact]
    public void UsePostgresOutbox_Should_DeferConfiguration_When_OptionsHaveNotBeenResolved()
    {
        // arrange
        var services = new ServiceCollection();
        var configureCount = 0;

        // act
        services.AddMessageBus().AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(options =>
            {
                configureCount++;
                options.RequireTransaction = false;
            }));

        // assert
        Assert.Equal(0, configureCount);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        Assert.False(options.RequireTransaction);
        Assert.Equal(1, configureCount);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterOnce_When_ConfigurationIsRepeated()
    {
        // arrange
        var services = CreateServices();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UseOutboxCore());

        // act
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        // assert
        Assert.Single(provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>());
        Assert.IsType<PostgresMessageOutbox>(scope.ServiceProvider.GetRequiredService<IMessageOutbox>());
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_ThrowClearError_When_ContextIsNotRegistered()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox()).AddInMemory();
        await using var provider = services.BuildServiceProvider();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext)));

        // assert
        Assert.Equal(
            $"The DbContext '{typeof(TestDbContext).FullName}' is not registered as a service. "
            + "Register it with AddDbContext, AddDbContextPool, AddDbContextFactory or AddPooledDbContextFactory. "
            + "Before EF Core 10, AddPooledDbContextFactory does not register the context itself; also add "
            + "services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TestDbContext>>()"
            + ".CreateDbContext()).",
            exception.Message);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterHostedService_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var hostedServices = provider.GetServices<IHostedService>();

        // Assert
        Assert.Contains(hostedServices, s => s is PostgresMessageBusOutboxWorker);
    }

    [Fact]
    public async Task StartAsync_Should_NotThrow_When_CalledMultipleTimes()
    {
        // arrange
        await using var provider = BuildProvider();
        var worker = provider.GetServices<IHostedService>()
            .OfType<PostgresMessageBusOutboxWorker>()
            .Single();

        // act
        await worker.StartAsync(CancellationToken.None);
        await worker.StartAsync(CancellationToken.None);

        // assert
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterScopedOutbox_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        using var scope = provider.CreateScope();
        var outbox = scope.ServiceProvider.GetService<IMessageOutbox>();

        // Assert
        Assert.NotNull(outbox);
        Assert.IsType<PostgresMessageOutbox>(outbox);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterKeyedSingletonProcessor_When_Called()
    {
        // arrange
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();

        // act
        var processor = provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext));

        // assert
        Assert.Same(
            processor,
            scope.ServiceProvider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext)));
        Assert.Null(provider.GetService<PostgresOutboxProcessor>());
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_ConfigureQueriesFromModel_When_DefaultTableNames()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();
        var contextName = typeof(TestDbContext).FullName;
        var options = optionsMonitor.Get(contextName);
        await using var scope = provider.CreateAsyncScope();
        await using var connection = options.CreateConnection(scope.ServiceProvider);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.InsertEnvelope));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.NextPollingInterval));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.ProcessEvent));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.DeleteEvent));
        Assert.Equal(ConnectionString, connection.ConnectionString);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = CreateServices();
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));

        return services;
    }
}
