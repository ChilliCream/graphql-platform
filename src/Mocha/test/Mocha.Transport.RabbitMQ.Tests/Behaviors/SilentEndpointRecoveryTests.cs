using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class SilentEndpointRecoveryTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task Bus_Should_Recover_When_EndpointAcceptsConnectionsWithoutAnswering()
    {
        // arrange
        // the bus reaches the broker through a proxy, as it would through a load balancer or a sidecar
        await using var vhost = await fixture.CreateVhostAsync();
        await using var proxy = new TcpProxy(vhost.ConnectionSettings.HostName, vhost.ConnectionSettings.Port);
        var throughProxy = new ConnectionFactory
        {
            Uri = vhost.ConnectionSettings.Uri,
            VirtualHost = vhost.VhostName,
            HostName = IPAddress.Loopback.ToString(),
            Port = proxy.Port
        };
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton<IConnectionFactory>(throughProxy)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        bus.Transport.Dispatcher.Connection.AttemptTimeout = TimeSpan.FromSeconds(2);
        bus.Transport.ConsumerManager.Connection.AttemptTimeout = TimeSpan.FromSeconds(2);
        await PublishAsync(bus, "ORD-1");
        await Wait.UntilAsync(() => recorder.Messages.Count == 1);

        // the broker delivers an event again whose acknowledgement it did not receive before the connection was lost
        await vhost.Broker.WaitForEmptyQueueAsync("orders");

        // act
        // both connections are lost, and the first attempt of each to reconnect gets no answer
        proxy.IsSilent = true;
        proxy.DropConnections();
        await Wait.UntilAsync(() => proxy.UnansweredConnections >= 2);
        proxy.IsSilent = false;
        await PublishAsync(bus, "ORD-2");
        await Wait.UntilAsync(() => recorder.Messages.Count == 2);
        await Wait.UntilAsync(() => bus.Transport.HealthFailureReason is null);

        // assert
        Assert.Equal(
            ["ORD-1", "ORD-2"],
            recorder.Messages.Cast<OrderCreated>().Select(order => order.OrderId).Order());
    }

    private static async Task PublishAsync(TestBus bus, string orderId)
    {
        using var scope = bus.Provider.CreateScope();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(new OrderCreated { OrderId = orderId }, deadline.Token);
    }
}
