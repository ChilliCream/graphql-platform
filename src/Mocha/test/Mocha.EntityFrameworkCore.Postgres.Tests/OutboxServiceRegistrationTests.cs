using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public async Task UsePostgresOutbox_Should_RejectMissingContext_When_PersistenceIsResolved()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => options.Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(
            """
            The messaging DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.Helpers.TestDbContext' is not registered. Register the context or an IDbContextFactory for it before building the service provider.
            """,
            exception.Message);
    }

    [Theory]
    [InlineData("Context", false)]
    [InlineData("Context", true)]
    [InlineData("Pool", false)]
    [InlineData("Pool", true)]
    [InlineData("Factory", false)]
    [InlineData("Factory", true)]
    [InlineData("PooledFactory", false)]
    [InlineData("PooledFactory", true)]
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
        var contextDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(TestDbContext));
        var factoryDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>));

        if (!messagingFirst)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        // act
        var context = scope.ServiceProvider.GetRequiredKeyedService<DbContextAccessor>(typeof(TestDbContext)).Context;
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        await using var connection = options.CreateConnection(scope.ServiceProvider);

        // assert
        Assert.Same(contextDescriptor, services.SingleOrDefault(s => s.ServiceType == typeof(TestDbContext)));
        Assert.Same(
            factoryDescriptor,
            services.SingleOrDefault(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>)));
        Assert.Same(context, context.Outbox.Context);
        Assert.Equal(ConnectionString, connection.ConnectionString);
        Assert.Contains("outbox_messages", options.Queries.InsertEnvelope);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DbContextAccessor_Should_DisposeContextOnce_When_ScopeIsDisposed(
        bool factoryOnly,
        bool asynchronously)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContextFactory<TrackedDbContext>(o => o.UseTestNpgsql(ConnectionString));

        if (factoryOnly)
        {
            services.RemoveAll<TrackedDbContext>();
        }

        services.AddMessageBus().AddEntityFramework<TrackedDbContext>(_ => { });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var scope = provider.CreateAsyncScope();
        var accessor = scope.ServiceProvider.GetRequiredKeyedService<DbContextAccessor>(typeof(TrackedDbContext));
        var secondAccessor = scope.ServiceProvider.GetRequiredKeyedService<DbContextAccessor>(typeof(TrackedDbContext));
        var context = (TrackedDbContext)accessor.Context;

        // act
        if (asynchronously)
        {
            await scope.DisposeAsync();
        }
        else
        {
            scope.Dispose();
        }

        // assert
        Assert.Same(accessor.Context, secondAccessor.Context);
        Assert.Equal(1, context.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsePostgresOutbox_Should_RegisterOnce_When_ConfigurationIsRepeated(bool separateBuilder)
    {
        // arrange
        var services = CreateServices();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UseOutboxCore());

        // act
        builder.AddEntityFramework<TestDbContext>(ef =>
        {
            ef.UsePostgresOutbox();

            if (!separateBuilder)
            {
                ef.UsePostgresOutbox();
            }
        });
        var contextDescriptor = services.Single(s => s.ServiceType == typeof(TestDbContext));

        if (separateBuilder)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var interceptors = context.GetInfrastructure().GetServices<IInterceptor>();

        // assert
        Assert.Same(contextDescriptor, services.Single(s => s.ServiceType == typeof(TestDbContext)));
        Assert.Single(provider.GetServices<PostgresOutboxProcessor>());
        Assert.Single(provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>());
        Assert.Equal(
            [typeof(OutboxDbTransactionInterceptor), typeof(OutboxSaveChangesInterceptor), typeof(PostgresOutboxTracker)],
            interceptors.Select(i => i.GetType()));
    }

    [Fact]
    public void UsePostgresOutbox_Should_RejectDifferentContext_When_OutboxIsAlreadyRegistered()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());

        // act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.AddEntityFramework<OtherDbContext>(ef => ef.UsePostgresOutbox()));

        // assert
        Assert.Equal(
            """
            An outbox is already registered for DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.Helpers.TestDbContext'. Registering an outbox for another DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.OutboxServiceRegistrationTests+OtherDbContext' is not supported.
            """,
            exception.Message);
    }

    [Fact]
    public async Task AddEntityFramework_Should_AllowDifferentContext_When_ItDoesNotEnableAnOutbox()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());

        // act
        builder.AddEntityFramework<OtherDbContext>(_ => { });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var otherContext = scope.ServiceProvider.GetRequiredService<OtherDbContext>();

        // assert
        Assert.Same(context, context.Outbox.Context);
        Assert.Empty(otherContext.GetInfrastructure().GetServices<IInterceptor>());
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task UsePostgresOutbox_Should_PreserveFactoryLifetime_When_ContextUsesFactory(
        ServiceLifetime factoryLifetime)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString), factoryLifetime);
        var factoryDescriptor = services.Single(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>));
        var contextDescriptor = services.Single(s => s.ServiceType == typeof(TestDbContext));

        // act
        services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>();
        var secondFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // assert
        Assert.Same(factoryDescriptor, services.Single(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>)));
        Assert.Same(contextDescriptor, services.Single(s => s.ServiceType == typeof(TestDbContext)));
        Assert.Equal(factoryLifetime != ServiceLifetime.Transient, ReferenceEquals(factory, secondFactory));
        Assert.Same(context, context.Outbox.Context);
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
    public async Task UsePostgresOutbox_Should_RegisterProcessor_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var processor = provider.GetService<PostgresOutboxProcessor>();

        // Assert
        Assert.NotNull(processor);
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

        // Build the runtime so that all singleton factories resolve
        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));

        // Use a resilient signal to prevent ObjectDisposedException when
        // EF Core shares the internal service provider (and interceptors)
        // across test classes via ShouldUseSameServiceProvider.
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        return services;
    }

    private static void RegisterContext(ServiceCollection services, string registration)
    {
        switch (registration)
        {
            case "Context":
                services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                break;

            case "Pool":
                services.AddDbContextPool<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                break;

            case "Factory":
                services.AddDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                services.RemoveAll<TestDbContext>();
                break;

            case "PooledFactory":
                services.AddPooledDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                services.RemoveAll<TestDbContext>();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(registration));
        }
    }

    private sealed class TrackedDbContext(DbContextOptions<TrackedDbContext> options) : DbContext(options)
    {
        public int DisposeCount { get; private set; }

        public override void Dispose()
        {
            DisposeCount++;
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            DisposeCount++;
            return base.DisposeAsync();
        }
    }

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options);
}
