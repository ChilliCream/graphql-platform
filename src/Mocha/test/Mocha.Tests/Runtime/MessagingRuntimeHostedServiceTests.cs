using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public sealed class MessagingRuntimeHostedServiceTests
{
    [Fact]
    public async Task StopAsync_Should_StopTransportsAndEndpoints_When_HostStops()
    {
        // arrange
        await using var provider = CreateProvider();
        var hostedService = GetHostedService(provider);
        var runtime = GetRuntime(provider);
        await hostedService.StartAsync(TestContext.Current.CancellationToken);

        // act
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.False(runtime.IsStarted);
        Assert.All(runtime.Transports, transport => Assert.False(transport.IsStarted));
        Assert.All(
            runtime.Transports.SelectMany(transport => transport.ReceiveEndpoints),
            endpoint => Assert.False(endpoint.IsStarted));
    }

    [Fact]
    public async Task StartAsync_Should_Throw_When_HostWasStopped()
    {
        // arrange
        await using var provider = CreateProvider();
        var hostedService = GetHostedService(provider);
        await hostedService.StartAsync(TestContext.Current.CancellationToken);
        await hostedService.StopAsync(TestContext.Current.CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("Messaging runtime is stopped and cannot be started again", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_Should_StopTransports_When_ServiceProviderIsDisposed()
    {
        // arrange
        var provider = CreateProvider();
        var runtime = GetRuntime(provider);
        await GetHostedService(provider).StartAsync(TestContext.Current.CancellationToken);

        // act
        await provider.DisposeAsync();

        // assert
        Assert.False(runtime.IsStarted);
        Assert.All(runtime.Transports, transport => Assert.False(transport.IsStarted));
    }

    private static ServiceProvider CreateProvider()
        => new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<EventHandler>()
            .AddBatchHandler<BatchHandler>(o => o.MaxBatchSize = 1)
            .AddInMemory(t =>
            {
                t.Queue("events").Handler<EventHandler>();
                t.Queue("batches").Handler<BatchHandler>();
            })
            .Services.BuildServiceProvider();

    private static MessagingRuntimeHostedService GetHostedService(IServiceProvider provider)
        => provider.GetServices<IHostedService>().OfType<MessagingRuntimeHostedService>().Single();

    private static MessagingRuntime GetRuntime(IServiceProvider provider)
        => (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

    public sealed record TestEvent(string Name);

    public sealed class EventHandler : IEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(TestEvent message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class BatchHandler : IBatchEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(IMessageBatch<TestEvent> messages, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
