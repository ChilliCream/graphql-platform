using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

#if NET9_0_OR_GREATER
    [Theory]
    [InlineData("Context", true)]
    [InlineData("PooledFactory", false)]
    public async Task UsePostgresOutbox_Should_ResolveContext_When_RegistrationOrderVaries(
        string registration,
        bool messagingFirst)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();
        var builder = services.AddMessageBus();
        builder.AddInMemory();

        if (messagingFirst)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        RegisterContext(services, registration);

        if (!messagingFirst)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        await using var provider = await StartAsync(services);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);

        // act
        await context.Outbox(bus).PublishAsync(new RegisteredEvent(1), TestToken);
        await using var connection = options.CreateConnection(scope.ServiceProvider);

        // assert
        Assert.Single(context.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(ConnectionString, connection.ConnectionString);
    }
#endif

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

#if NET9_0_OR_GREATER
    [Fact]
    public async Task AddEntityFramework_Should_AllowDifferentContext_When_ItDoesNotEnableAnOutbox()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEntityFramework<OtherDbContext>(_ => { });
        await using var provider = await StartAsync(services);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var otherContext = scope.ServiceProvider.GetRequiredService<OtherDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await context.Outbox(bus).PublishAsync(new RegisteredEvent(1), TestToken);
        var exception = Assert.Throws<InvalidOperationException>(() => otherContext.Outbox(bus));

        // assert
        Assert.Single(context.ChangeTracker.Entries<OutboxMessage>());
        Assert.Equal(
            $"The DbContext '{typeof(OtherDbContext).FullName}' has no outbox. "
            + $"Register one with AddEntityFramework<{nameof(OtherDbContext)}>(x => x.UsePostgresOutbox()) "
            + "and create the context through dependency injection (AddDbContext, AddDbContextFactory, "
            + "AddDbContextPool or AddPooledDbContextFactory), or construct it with options resolved from "
            + "dependency injection; options built by hand are not supported.",
            exception.Message);
    }
#endif

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

#if NET9_0_OR_GREATER
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    private static async Task<ServiceProvider> StartAsync(ServiceCollection services)
    {
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        return provider;
    }
#endif

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
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        return services;
    }

#if NET9_0_OR_GREATER
    private static void RegisterContext(ServiceCollection services, string registration)
    {
        switch (registration)
        {
            case "Context":
                services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                break;

            case "PooledFactory":
                services.AddPooledDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
#if !NET10_0_OR_GREATER
                // Before EF Core 10, AddPooledDbContextFactory does not register the context itself.
                services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext());
#endif
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(registration));
        }
    }
#endif

    public sealed record RegisteredEvent(int Id);

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.AddPostgresOutbox();
    }
}
