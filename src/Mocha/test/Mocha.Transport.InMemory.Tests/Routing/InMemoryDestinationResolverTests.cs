using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Routing;

public class InMemoryDestinationResolverTests
{
    [Fact]
    public void ResolveDestination_Should_UseConventionTopic_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var resolver = new InMemoryDestinationResolver(InMemoryTransportConfiguration.DefaultSchema);
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var resolution = resolver.ResolveDestination(runtime.Naming, route);

        // assert
        Assert.Equal(InMemoryDestinationKind.Topic, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
        Assert.Equal("t/" + expectedName, resolution.EndpointName);
    }

    [Fact]
    public void ResolveDestination_Should_ResolveExplicitTopic_When_DestinationIsTopic()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToInMemoryTopic("orders-topic"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var resolver = new InMemoryDestinationResolver(InMemoryTransportConfiguration.DefaultSchema);

        // act
        var resolution = resolver.ResolveDestination(runtime.Naming, route);

        // assert
        Assert.Equal(InMemoryDestinationKind.Topic, resolution.Kind);
        Assert.Equal("orders-topic", resolution.Name);
        Assert.Equal("t/orders-topic", resolution.EndpointName);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddInMemory()
            .BuildRuntime();
        return runtime;
    }
}
