using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Health;

public sealed class TransportStartedHealthCheckTests
{
    [Fact]
    public async Task CheckAsync_Should_ReportTransport_When_TransportIsNotStarted()
    {
        // arrange
        await using var provider = CreateBus();
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var check = Assert.Single(runtime.Services.GetServices<IMessageBusHealthCheck>());
        var context = new MessageBusHealthContext();

        // act
        await check.CheckAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([new MessageBusHealthFailure("orders", "Transport 'orders' is not started.")], context.Failures);
    }

    [Fact]
    public async Task CheckAsync_Should_ReportNothing_When_TransportAndEndpointsAreStarted()
    {
        // arrange
        await using var provider = CreateBus();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var check = Assert.Single(runtime.Services.GetServices<IMessageBusHealthCheck>());
        var context = new MessageBusHealthContext();

        // act
        await check.CheckAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal([], context.Failures);
    }

    [Fact]
    public async Task CheckAsync_Should_ReportEndpoint_When_ReceiveEndpointIsStopped()
    {
        // arrange
        await using var provider = CreateBus();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);
        var endpoint = runtime.Transports.Single().ReceiveEndpoints.First();
        await endpoint.StopAsync(runtime, TestContext.Current.CancellationToken);
        var check = Assert.Single(runtime.Services.GetServices<IMessageBusHealthCheck>());
        var context = new MessageBusHealthContext();

        // act
        await check.CheckAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            [
                new MessageBusHealthFailure(
                    "orders",
                    $"Receive endpoint '{endpoint.Name}' of transport 'orders' is not started.")
            ],
            context.Failures);
    }

    private static ServiceProvider CreateBus()
    {
        var services = new ServiceCollection();
        services.AddMessageBus().AddInMemory(t => t.Name("orders"));
        return services.BuildServiceProvider();
    }
}
