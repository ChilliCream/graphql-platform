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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UsePostgresOutbox_Should_RejectInvalidConcurrency_When_OptionsResolved(int concurrency)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(o => o.MaxConcurrentMessages = concurrency));
        using var provider = services.BuildServiceProvider();

        // act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
                .Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(["MaxConcurrentMessages must be greater than zero."], exception.Failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(25 * 24 * 60 * 60 * 1000L)]
    public void UsePostgresOutbox_Should_RejectInvalidDispatchTimeout_When_OptionsResolved(long milliseconds)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(o => o.DispatchTimeout = TimeSpan.FromMilliseconds(milliseconds)));
        using var provider = services.BuildServiceProvider();

        // act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
                .Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(
            ["DispatchTimeout must be greater than zero and at most 24 days, or Timeout.InfiniteTimeSpan."],
            exception.Failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UsePostgresOutbox_Should_RejectInvalidMaxAttempts_When_OptionsResolved(int maxAttempts)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(o => o.MaxAttempts = maxAttempts));
        using var provider = services.BuildServiceProvider();

        // act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
                .Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(["MaxAttempts must be greater than zero."], exception.Failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UsePostgresOutbox_Should_RejectInvalidMaxRetryDelay_When_OptionsResolved(int milliseconds)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(o => o.MaxRetryDelay = TimeSpan.FromMilliseconds(milliseconds)));
        using var provider = services.BuildServiceProvider();

        // act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
                .Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(["MaxRetryDelay must be greater than zero."], exception.Failures);
    }

    [Fact]
    public void UsePostgresOutbox_Should_UseDefaults_When_OptionsAreNotConfigured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddMessageBus().AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        using var provider = services.BuildServiceProvider();

        // act
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.DispatchTimeout);
        Assert.Equal(10, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromMinutes(10), options.MaxRetryDelay);
    }

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
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));

        // Use a resilient signal to prevent ObjectDisposedException when
        // EF Core shares the internal service provider (and interceptors)
        // across test classes via ShouldUseSameServiceProvider.
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        // Build the runtime so that all singleton factories resolve
        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }
}
