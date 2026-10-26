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
    [InlineData("FactoryOnly", false)]
    [InlineData("FactoryOnly", true)]
    [InlineData("PooledFactoryOnly", false)]
    [InlineData("PooledFactoryOnly", true)]
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
        Assert.Equal(
            contextDescriptor?.Lifetime ?? ServiceLifetime.Scoped,
            services.SingleOrDefault(s => s.ServiceType == typeof(TestDbContext))?.Lifetime ?? ServiceLifetime.Scoped);
        Assert.Same(
            factoryDescriptor,
            services.SingleOrDefault(s => s.ServiceType == typeof(IDbContextFactory<TestDbContext>)));
        Assert.Same(context.GetService<DbContextOutbox>(), context.Outbox);
        Assert.Equal(ConnectionString, connection.ConnectionString);
        Assert.Same(scope.ServiceProvider.GetRequiredService<IMessageBus>(), context.Outbox.GetBus());
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
        Assert.Same(
            scope.ServiceProvider.GetRequiredService<IMessageBus>(),
            context.Outbox.GetBus());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UsePostgresOutbox_Should_LeaveFactoryContextsUnattached_When_CreatedDirectly(
        bool pooled,
        bool messagingFirst)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        if (messagingFirst)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        RegisterContext(services, pooled ? "PooledFactory" : "Factory");
        if (!messagingFirst)
        {
            builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        }

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // act
        var exception = Assert.Throws<InvalidOperationException>(context.Outbox.GetBus);
        context.UseOutbox(bus);

        // assert
        Assert.Equal(
            "The outbox for 'TestDbContext' is not attached to a messaging scope. Call db.UseOutbox(bus) before publishing.",
            exception.Message);
        Assert.Same(bus, context.Outbox.GetBus());
    }

    [Theory]
    [InlineData("Pool")]
    [InlineData("PooledFactory")]
    public async Task UsePostgresOutbox_Should_AttachCurrentScope_When_PooledContextIsReused(string registration)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        RegisterContext(services, registration);
        services.AddMessageBus()
            .AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox())
            .AddInMemory();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        TestDbContext firstContext;
        IMessageBus firstBus;
        await using (var firstScope = provider.CreateAsyncScope())
        {
            firstContext = firstScope.ServiceProvider.GetRequiredService<TestDbContext>();
            firstBus = firstContext.Outbox.GetBus();
        }

        // act
        await using var secondScope = provider.CreateAsyncScope();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<TestDbContext>();
        var secondBus = secondContext.Outbox.GetBus();

        // assert
        Assert.Same(firstContext, secondContext);
        Assert.NotSame(firstBus, secondBus);
        Assert.Same(secondScope.ServiceProvider.GetRequiredService<IMessageBus>(), secondBus);
    }

    [Fact]
    public void UsePostgresOutbox_Should_RejectSingletonContext_When_RegisteredForOutbox()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString), ServiceLifetime.Singleton);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox()));

        // assert
        Assert.Equal(
            "The outbox DbContext 'Mocha.EntityFrameworkCore.Postgres.Tests.Helpers.TestDbContext' cannot be registered as a singleton. Register it with a scoped or transient lifetime.",
            exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UsePostgresOutbox_Should_ValidateExistingAttachment_When_CustomScopedFactoryCreatesContext(
        bool sameScope)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<AttachedDbContext>(options => options.UseTestNpgsql(ConnectionString));
        IMessageBus otherBus = null!;
        AttachedDbContext createdContext = null!;
        services.Replace(ServiceDescriptor.Scoped(sp =>
        {
            createdContext = sp.GetRequiredService<IDbContextFactory<AttachedDbContext>>().CreateDbContext();
            createdContext.UseOutbox(sameScope ? sp.GetRequiredService<IMessageBus>() : otherBus);
            return createdContext;
        }));
        services.AddMessageBus()
            .AddEntityFramework<AttachedDbContext>(ef => ef.UsePostgresOutbox())
            .AddInMemory();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var otherScope = provider.CreateAsyncScope();
        otherBus = otherScope.ServiceProvider.GetRequiredService<IMessageBus>();
        await using var scope = provider.CreateAsyncScope();

        // act
        if (sameScope)
        {
            var context = scope.ServiceProvider.GetRequiredService<AttachedDbContext>();

            // assert
            Assert.Same(
                scope.ServiceProvider.GetRequiredService<IMessageBus>(),
                context.Outbox.GetBus());
        }
        else
        {
            var exception = Assert.Throws<InvalidOperationException>(
                scope.ServiceProvider.GetRequiredService<AttachedDbContext>);

            // assert
            Assert.Equal("This DbContext is already attached to a different messaging scope.", exception.Message);
            Assert.Equal(1, createdContext.DisposeCount);
            Assert.Same(otherBus, otherScope.ServiceProvider.GetRequiredService<IMessageBus>());
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task UsePostgresOutbox_Should_DisposeContextOnce_When_ScopeIsDisposed(
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
            [typeof(OutboxDbTransactionInterceptor), typeof(OutboxSaveChangesInterceptor), typeof(PostgresOutboxTracker)],
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
            context.GetService<IDbContextOutboxProvider>(),
            otherContext.GetService<IDbContextOutboxProvider>());
        Assert.Same(otherContext.GetService<DbContextOutbox>(), otherContext.Outbox);
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
            Multiple outbox DbContext types are registered. Call UseDefaultOutbox<TContext>() to select the context for publishing through IMessageBus, or publish through db.Outbox to select a context instance.
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
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());

        // act
        builder.AddEntityFramework<OtherDbContext>(_ => { });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var otherContext = scope.ServiceProvider.GetRequiredService<OtherDbContext>();

        // assert
        Assert.Same(context.GetService<DbContextOutbox>(), context.Outbox);
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
        Assert.Equal(contextDescriptor.Lifetime, services.Single(s => s.ServiceType == typeof(TestDbContext)).Lifetime);
        Assert.Equal(factoryLifetime != ServiceLifetime.Transient, ReferenceEquals(factory, secondFactory));
        Assert.Same(context.GetService<DbContextOutbox>(), context.Outbox);
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
        var contextDescriptor = services.LastOrDefault(s => s.ServiceType == typeof(TestDbContext));

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

            case "FactoryOnly":
                services.AddDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                RemoveFactoryContextRegistration();
                break;

            case "PooledFactoryOnly":
                services.AddPooledDbContextFactory<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));
                RemoveFactoryContextRegistration();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(registration));
        }

        void RemoveFactoryContextRegistration()
        {
            services.RemoveAll<TestDbContext>();
            if (contextDescriptor is not null)
            {
                services.Add(contextDescriptor);
            }
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

    private sealed class AttachedDbContext(DbContextOptions<AttachedDbContext> options) : DbContext(options)
    {
        public int DisposeCount { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddPostgresOutbox();

        public override void Dispose()
        {
            DisposeCount++;
            base.Dispose();
        }
    }
}
