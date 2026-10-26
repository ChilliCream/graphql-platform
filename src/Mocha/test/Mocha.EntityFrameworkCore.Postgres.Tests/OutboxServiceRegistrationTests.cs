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
            No service for type 'Mocha.EntityFrameworkCore.Postgres.Tests.Helpers.TestDbContext' has been registered.
            """,
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseOutboxCore_Should_RegisterInterceptors_When_EnabledAfterConfigurationCallback(bool usePostgresOutbox)
    {
        // arrange
        var services = CreateServices();
        IEntityFrameworkCoreBuilder efBuilder = null!;
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => efBuilder = ef)
            .AddInMemory();

        // act
        if (usePostgresOutbox)
        {
            efBuilder.UsePostgresOutbox();
        }
        else
        {
            efBuilder.UseOutboxCore();
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // assert
        Assert.Equal(
            usePostgresOutbox ? 3 : 2,
            context.GetInfrastructure().GetServices<IInterceptor>().Count());
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
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        await using var connection = options.CreateConnection(scope.ServiceProvider);

        // assert
        Assert.Same(contextDescriptor, services.Single(s => s.ServiceType == typeof(TestDbContext)));
        Assert.Same(
            factoryDescriptor,
            services.SingleOrDefault(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>)));
        Assert.IsType<PostgresDbContextOutbox>(context.GetService<IDbContextOutbox>());
        Assert.Equal(ConnectionString, connection.ConnectionString);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsePostgresOutbox_Should_SelectRegisteredOptions_When_ContextHasMultipleConstructors(
        bool messagingFirst)
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        if (messagingFirst)
        {
            builder.AddEntityFramework<MultiConstructorDbContext>(ef => ef.UsePostgresOutbox());
        }

        services.AddDbContext<MultiConstructorDbContext>(options => options.UseTestNpgsql(ConnectionString));
        if (!messagingFirst)
        {
            builder.AddEntityFramework<MultiConstructorDbContext>(ef => ef.UsePostgresOutbox());
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        // act
        var context = scope.ServiceProvider.GetRequiredService<MultiConstructorDbContext>();

        // assert
        Assert.True(context.UsedRegisteredOptions);
        Assert.Equal(ConnectionString, context.Database.GetConnectionString());
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_PreserveCustomFactory_When_ContextIsResolved()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContextFactory<TrackedDbContext>(options => options.UseTestNpgsql(ConnectionString));
        TrackedDbContext createdContext = null!;
        var descriptor = ServiceDescriptor.Scoped(sp =>
        {
            createdContext = sp.GetRequiredService<IDbContextFactory<TrackedDbContext>>().CreateDbContext();
            return createdContext;
        });
        services.Replace(descriptor);
        services.AddMessageBus().AddEntityFramework<TrackedDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var scope = provider.CreateAsyncScope();

        // act
        var context = scope.ServiceProvider.GetRequiredService<TrackedDbContext>();
        await scope.DisposeAsync();

        // assert
        Assert.Same(descriptor, services.Single(s => s.ServiceType == typeof(TrackedDbContext)));
        Assert.Same(createdContext, context);
        Assert.Equal(1, context.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsePostgresOutbox_Should_DisposeContextOnce_When_ScopeIsDisposed(bool asynchronously)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContextFactory<TrackedDbContext>(o => o.UseTestNpgsql(ConnectionString));

        services.AddMessageBus().AddEntityFramework<TrackedDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TrackedDbContext>();
        var secondContext = scope.ServiceProvider.GetRequiredService<TrackedDbContext>();

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
        Assert.Same(context, secondContext);
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
        Assert.Single(provider.GetKeyedServices<PostgresOutboxProcessor>(typeof(TestDbContext)));
        Assert.Single(provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>());
        Assert.Equal(
            [
                typeof(OutboxDbTransactionInterceptor),
                typeof(OutboxSaveChangesInterceptor),
                typeof(PostgresDbContextOutbox)
            ],
            interceptors.Select(i => i.GetType()));
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterIndependentProcessors_When_MultipleContextsAreConfigured()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());

        // act
        builder.AddEntityFramework<OtherDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var otherContext = scope.ServiceProvider.GetRequiredService<OtherDbContext>();

        // assert
        Assert.NotSame(
            provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(TestDbContext)),
            provider.GetRequiredKeyedService<PostgresOutboxProcessor>(typeof(OtherDbContext)));
        Assert.Equal(2, provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().Count());
        Assert.NotSame(
            context.GetService<IDbContextOutbox>(),
            otherContext.GetService<IDbContextOutbox>());
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_RejectAmbiguousBusOutbox_When_NoDefaultIsSelected()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox())
            .AddEntityFramework<OtherDbContext>(ef => ef.UsePostgresOutbox());
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        // act
        var exception = Assert.Throws<InvalidOperationException>(scope.ServiceProvider.GetRequiredService<IMessageOutbox>);

        // assert
        Assert.Equal(
            """
            Multiple outbox DbContext types are registered. Call UseDefaultOutbox<TContext>() to select the context for publishing through IMessageBus, or publish through db.Outbox(bus) to select a context instance.
            """,
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UseDefaultOutbox_Should_SelectContext_When_MultipleContextsAreRegistered(bool defaultFirst)
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        if (defaultFirst)
        {
            builder.UseDefaultOutbox<OtherDbContext>();
        }

        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddEntityFramework<OtherDbContext>(ef => ef.UsePostgresOutbox());
        if (!defaultFirst)
        {
            builder.UseDefaultOutbox<OtherDbContext>();
        }

        var configuredContexts = new List<string>();
        services.Configure<PostgresMessageOutboxOptions>(typeof(TestDbContext).FullName, _ =>
            configuredContexts.Add(nameof(TestDbContext)));
        services.Configure<PostgresMessageOutboxOptions>(typeof(OtherDbContext).FullName, _ =>
            configuredContexts.Add(nameof(OtherDbContext)));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();

        // act
        var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();

        // assert
        Assert.IsType<PostgresMessageOutbox>(outbox);
        Assert.Equal([nameof(OtherDbContext)], configuredContexts);
    }

    [Fact]
    public async Task AddEntityFramework_Should_AllowDifferentContext_When_ItDoesNotEnableAnOutbox()
    {
        // arrange
        var services = CreateServices();
        services.AddDbContext<OtherDbContext>(o => o.UseTestNpgsql(ConnectionString));
        var contextDescriptor = services.Single(s => s.ServiceType == typeof(OtherDbContext));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());

        // act
        builder.AddEntityFramework<OtherDbContext>(_ => { });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var otherContext = scope.ServiceProvider.GetRequiredService<OtherDbContext>();

        // assert
        Assert.Same(contextDescriptor, services.Single(s => s.ServiceType == typeof(OtherDbContext)));
        Assert.IsType<PostgresDbContextOutbox>(context.GetService<IDbContextOutbox>());
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
        Assert.IsType<PostgresDbContextOutbox>(context.GetService<IDbContextOutbox>());
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
                break;

            case "PooledFactory":
                services.AddPooledDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
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

    private sealed class MultiConstructorDbContext : DbContext
    {
        public MultiConstructorDbContext() { }

        public MultiConstructorDbContext(DbContextOptions<MultiConstructorDbContext> options) : base(options)
        {
            UsedRegisteredOptions = true;
        }

        public bool UsedRegisteredOptions { get; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddPostgresOutbox();
    }

    private sealed class OtherDbContext(DbContextOptions<OtherDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.AddPostgresOutbox();
    }
}
