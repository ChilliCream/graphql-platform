#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxRepeatedRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=test";

#if NET9_0_OR_GREATER
    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterEachInterceptorOnce_When_CalledTwiceForOneContext()
    {
        // arrange
        await using var provider = BuildProvider(
            ef => ef.UsePostgresOutbox(),
            ef => ef.UsePostgresOutbox());
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        var interceptors = dbContext.GetService<IEnumerable<IInterceptor>>()
            .Select(interceptor => interceptor.GetType().Name)
            .Order(StringComparer.Ordinal);

        // assert
        Assert.Equal(
            ["OutboxAtomicSaveInterceptor", "OutboxDbTransactionInterceptor", "OutboxSaveChangesInterceptor"],
            interceptors);
    }
#endif

    [Fact]
    public async Task UsePostgresOutbox_Should_RegisterOneProcessorWorkerAndValidator_When_CalledTwiceForOneContext()
    {
        // arrange
        await using var provider = BuildProvider(
            ef => ef.UsePostgresOutbox(),
            ef => ef.UsePostgresOutbox());

        // act
        var processors = provider.GetKeyedServices<PostgresOutboxProcessor>(typeof(TestDbContext)).Count();
        var workers = provider.GetServices<IHostedService>().OfType<PostgresMessageBusOutboxWorker>().Count();
        var validators = provider.GetServices<IValidateOptions<PostgresOutboxOptions>>().Count();

        // assert
        Assert.Equal(1, processors);
        Assert.Equal(1, workers);
        Assert.Equal(1, validators);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_ValidateSecondConfiguration_When_CalledTwiceForOneContext()
    {
        // arrange
        await using var provider = BuildProvider(
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.FromSeconds(1)),
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.Zero));
        var monitor = provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>();

        // act
        var exception = Assert.Throws<OptionsValidationException>(() => monitor.Get(typeof(TestDbContext).FullName));

        // assert
        Assert.Equal(
            "PostgresOutboxOptions.PollingInterval must be greater than zero and at most 49.17:02:47.2940000.",
            exception.Message);
    }

    [Fact]
    public async Task UsePostgresOutbox_Should_ApplySecondConfiguration_When_CalledTwiceForOneContext()
    {
        // arrange
        await using var provider = BuildProvider(
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.FromSeconds(1)),
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = TimeSpan.FromSeconds(2)));
        var monitor = provider.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>();

        // act
        var options = monitor.Get(typeof(TestDbContext).FullName);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(2), options.PollingInterval);
    }

    private static ServiceProvider BuildProvider(
        Action<IEntityFrameworkCoreBuilder> first,
        Action<IEntityFrameworkCoreBuilder> second)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(ConnectionString));
        var builder = services.AddMessageBus();
        builder.AddInMemory();
        builder.AddEntityFramework<TestDbContext>(first);
        builder.AddEntityFramework<TestDbContext>(second);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
