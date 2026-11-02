using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class SchedulingServiceRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=test";

    [Fact]
    public async Task UsePostgresScheduling_Should_RegisterHostedService_When_Called()
    {
        // arrange
        await using var provider = BuildProvider();

        // act
        var hostedServices = provider.GetServices<IHostedService>();

        // assert
        Assert.Contains(hostedServices, s => s is ScheduledMessageWorker);
    }

    [Fact]
    public async Task StartAsync_Should_NotThrow_When_CalledMultipleTimes()
    {
        // arrange
        await using var provider = BuildProvider();
        var worker = provider.GetServices<IHostedService>()
            .OfType<ScheduledMessageWorker>()
            .Single();

        // act
        await worker.StartAsync(CancellationToken.None);
        await worker.StartAsync(CancellationToken.None);

        // assert
        await worker.StopAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(ConnectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresScheduling());
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        // Build the runtime so that all singleton factories resolve
        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }
}
