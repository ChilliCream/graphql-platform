using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class AutoProvisionTests
{
    [Fact]
    public void AutoProvision_Should_BeTrue_When_NotConfigured()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(_ => { });

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void AutoProvision_Should_BeFalse_When_AutoProvisionSetToFalse()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.AutoProvision(false));

        // assert
        Assert.False(topology.AutoProvision);
    }

    [Fact]
    public void AutoProvision_Should_BeTrue_When_AutoProvisionSetToTrue()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.AutoProvision(true));

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void Exchange_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("ex1"));

        // assert - null means it inherits the topology default
        var exchange = topology.Exchanges.Single(e => e.Name == "ex1");
        Assert.Null(exchange.AutoProvision);
    }

    [Fact]
    public void Exchange_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(false);
            t.DeclareExchange("ex1").AutoProvision(true);
        });

        // assert - explicit true overrides topology false
        var exchange = topology.Exchanges.Single(e => e.Name == "ex1");
        Assert.True(exchange.AutoProvision);
    }

    [Fact]
    public void Exchange_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(true);
            t.DeclareExchange("ex1").AutoProvision(false);
        });

        // assert - explicit false overrides topology true
        var exchange = topology.Exchanges.Single(e => e.Name == "ex1");
        Assert.False(exchange.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("q1"));

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.Null(queue.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(false);
            t.DeclareQueue("q1").AutoProvision(true);
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.True(queue.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(true);
            t.DeclareQueue("q1").AutoProvision(false);
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.False(queue.AutoProvision);
    }

    [Fact]
    public void Binding_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("ex1");
            t.DeclareQueue("q1");
            t.DeclareBinding("ex1", "q1");
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.Null(binding.AutoProvision);
    }

    [Fact]
    public void Binding_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(false);
            t.DeclareExchange("ex1");
            t.DeclareQueue("q1");
            t.DeclareBinding("ex1", "q1").AutoProvision(true);
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.True(binding.AutoProvision);
    }

    [Fact]
    public void Binding_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.AutoProvision(true);
            t.DeclareExchange("ex1");
            t.DeclareQueue("q1");
            t.DeclareBinding("ex1", "q1").AutoProvision(false);
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.False(binding.AutoProvision);
    }

    [Fact]
    public void Describe_Should_IncludeAutoProvisionProperty_When_TransportEnabledByDefault()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - all entities should have autoProvision in their properties
        Assert.NotNull(description.Topology);
        foreach (var entity in description.Topology!.Entities)
        {
            Assert.True(
                entity.Properties!.ContainsKey("autoProvision"),
                $"Entity '{entity.Name}' should have autoProvision in properties");
        }

        // exchanges declared via convention get null AutoProvision → inherit topology default (true)
        var exchanges = description.Topology.Entities.Where(e => e.Kind == "exchange");
        Assert.All(exchanges, e =>
            Assert.True((bool)e.Properties!["autoProvision"]!,
                $"Exchange '{e.Name}' should report autoProvision=true"));
    }

    [Fact]
    public void Describe_Should_ReportAutoProvisionFalse_When_TopologyDisabled()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(t =>
        {
            t.AutoProvision(false);
            t.DeclareExchange("ex1");
            t.DeclareQueue("q1");
            t.DeclareBinding("ex1", "q1");
        });

        var description = transport.Describe();

        // assert - all entities and links should have autoProvision = false
        Assert.NotNull(description.Topology);
        foreach (var entity in description.Topology!.Entities.Where(e => e.Name is "ex1" or "q1"))
        {
            Assert.False(
                (bool)entity.Properties!["autoProvision"]!,
                $"Entity '{entity.Name}' should report autoProvision=false");
        }

        foreach (var link in description.Topology.Links)
        {
            Assert.False(
                (bool)link.Properties!["autoProvision"]!,
                "Binding link should report autoProvision=false");
        }
    }

    [Fact]
    public void Describe_Should_ReportMixedAutoProvision_When_ResourceOverridesTopology()
    {
        // arrange
        var (_, transport, _) = CreateTopology(t =>
        {
            t.AutoProvision(false);
            t.DeclareExchange("ex1").AutoProvision(true);
            t.DeclareQueue("q1");
            t.DeclareBinding("ex1", "q1");
        });

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);

        var exchangeEntity = description.Topology!.Entities.Single(e => e.Name == "ex1");
        Assert.True(
            (bool)exchangeEntity.Properties!["autoProvision"]!,
            "Exchange with explicit AutoProvision(true) should report true");

        var queueEntity = description.Topology.Entities.Single(e => e.Name == "q1");
        Assert.False(
            (bool)queueEntity.Properties!["autoProvision"]!,
            "Queue with null AutoProvision should inherit topology false");
    }

    [Fact]
    public void Convention_Should_PropagateAutoProvision_When_ReceiveEndpointCreated()
    {
        // arrange - the reply endpoint always sets AutoProvision = true
        var runtime = CreateRuntime(b =>
            b.AddRequestHandler<GetOrderStatusHandler>());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act - find the reply queue (temporary, auto-provisioned)
        var replyEndpoint = transport.ReceiveEndpoints
            .FirstOrDefault(e => e.Kind == ReceiveEndpointKind.Reply);

        // assert
        Assert.NotNull(replyEndpoint);
        var replyQueueName = ((RabbitMQReceiveEndpoint)replyEndpoint).Queue?.Name;
        Assert.NotNull(replyQueueName);

        var replyQueue = topology.Queues.Single(q => q.Name == replyQueueName);
        Assert.True(replyQueue.AutoProvision);
    }

    [Fact]
    public void Descriptor_AutoProvision_Should_ReturnSelf_When_Chaining()
    {
        // arrange & act - just verify the builder compiles and chains correctly
        var (_, transport, _) = CreateTopology(t =>
            t.AutoProvision(false)
             .DeclareExchange("ex1")
             .AutoProvision(true));

        // assert - the last call wins
        var exchange = ((RabbitMQMessagingTopology)transport.Topology)
            .Exchanges.Single(e => e.Name == "ex1");
        Assert.True(exchange.AutoProvision);
    }

    private static (
        MessagingRuntime Runtime,
        RabbitMQMessagingTransport Transport,
        RabbitMQMessagingTopology Topology) CreateTopology(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.AddRabbitMQ(t => t.ConnectionProvider(_ => new StubConnectionProvider())).BuildRuntime();
        return runtime;
    }
}
