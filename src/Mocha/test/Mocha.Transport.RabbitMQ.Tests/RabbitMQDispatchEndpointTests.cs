using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

public class RabbitMQDispatchEndpointTests
{
    [Fact]
    public void DispatchEndpoint_Should_TargetQueue_When_QueueConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Queue);
        Assert.Equal("my-q", endpoint.Queue.Name);
        Assert.Null(endpoint.Exchange);
        Assert.IsType<RabbitMQQueue>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetExchange_When_ExchangeConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("my-ex");
            t.DispatchEndpoint("ep").ToExchange("my-ex");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-ex" });

        // assert
        Assert.NotNull(endpoint.Exchange);
        Assert.Equal("my-ex", endpoint.Exchange.Name);
        Assert.Null(endpoint.Queue);
        Assert.IsType<RabbitMQExchange>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveCorrectName_When_QueueConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.Equal("ep", endpoint.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveCorrectName_When_ExchangeConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("my-ex");
            t.DispatchEndpoint("ep").ToExchange("my-ex");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-ex" });

        // assert
        Assert.Equal("ep", endpoint.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_QueueConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("q/my-q", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_ExchangeConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("my-ex");
            t.DispatchEndpoint("ep").ToExchange("my-ex");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-ex" });

        // assert
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("e/my-ex", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareQueue("my-q").AutoProvision(true);
            t.DispatchEndpoint("ep").ToQueue("my-q");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Queue is { Name: "my-q" });

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("rabbitmq", endpoint.Address.Scheme);
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_ReplyPathContainsExchange()
    {
        // arrange - create a reply dispatch endpoint and an exchange dispatch endpoint
        var runtime = CreateRuntime(t =>
        {
            t.DeclareExchange("my-exchange");
            t.DispatchEndpoint("ep").ToExchange("my-exchange");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-exchange" });

        // assert - the destination address contains the e/ exchange path format
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("e/my-exchange", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_ReplyPathContainsExchangeWithVhost()
    {
        // arrange - use a custom vhost via stub
        var runtime = CreateRuntimeWithVhost(t =>
        {
            t.DeclareExchange("my-exchange");
            t.DispatchEndpoint("ep").ToExchange("my-exchange");
        });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport
            .DispatchEndpoints.OfType<RabbitMQDispatchEndpoint>()
            .First(e => e.Exchange is { Name: "my-exchange" });

        // assert - with vhost the address includes the vhost segment before e/
        var address = endpoint.Destination.Address.ToString();
        Assert.Contains("e/my-exchange", address);
        Assert.Contains("myvhost", address);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedQueue_When_QueueNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:q/reporting.events"));

        // assert
        var rabbitEndpoint = Assert.IsType<RabbitMQDispatchEndpoint>(endpoint);
        Assert.NotNull(rabbitEndpoint.Queue);
        Assert.False(rabbitEndpoint.Queue.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, rabbitEndpoint.Queue.Origin);
        Assert.Same(rabbitEndpoint.Queue, rabbitEndpoint.Destination);
        RabbitMQDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "rabbitmq",
              "TransportType": "RabbitMQMessagingTransport",
              "Entities": [
                {
                  "Kind": "queue",
                  "Name": "reporting.events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedExchange_When_ExchangeNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:e/reporting-events"));

        // assert
        var rabbitEndpoint = Assert.IsType<RabbitMQDispatchEndpoint>(endpoint);
        Assert.NotNull(rabbitEndpoint.Exchange);
        Assert.False(rabbitEndpoint.Exchange.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, rabbitEndpoint.Exchange.Origin);
        Assert.Same(rabbitEndpoint.Exchange, rabbitEndpoint.Destination);
        RabbitMQDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "rabbitmq",
              "TransportType": "RabbitMQMessagingTransport",
              "Entities": [
                {
                  "Kind": "exchange",
                  "Name": "reporting-events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ReuseEndpoint_When_QueueOutsideTopologyAddressedByOtherForms()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:q/reporting.events"));

        // act
        var byTopologyAddress = runtime.GetDispatchEndpoint(new Uri("rabbitmq://localhost:5672/q/reporting.events"));
        var byQueueScheme = runtime.GetDispatchEndpoint(new Uri("queue:reporting.events"));

        // assert
        Assert.Same(endpoint, byTopologyAddress);
        Assert.Same(endpoint, byQueueScheme);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ReuseEndpoint_When_ExchangeOutsideTopologyAddressedByOtherForms()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:e/reporting-events"));

        // act
        var byTopologyAddress = runtime.GetDispatchEndpoint(new Uri("rabbitmq://localhost:5672/e/reporting-events"));
        var byExchangeScheme = runtime.GetDispatchEndpoint(new Uri("exchange:reporting-events"));

        // assert
        Assert.Same(endpoint, byTopologyAddress);
        Assert.Same(endpoint, byExchangeScheme);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_BindTopologyQueue_When_QueueDeclaredWithoutProvisioning()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.DeclareQueue("reporting.events").AutoProvision(false);
        });

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("rabbitmq:q/reporting.events"));

        // assert
        var rabbitEndpoint = Assert.IsType<RabbitMQDispatchEndpoint>(endpoint);
        Assert.NotNull(rabbitEndpoint.Queue);
        Assert.False(rabbitEndpoint.Queue.AutoProvision);
        Assert.Same(rabbitEndpoint.Queue, rabbitEndpoint.Destination);
    }

    private static MessagingRuntime CreateRuntimeWithVhost(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new VhostStubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }

    private sealed class VhostStubConnectionProvider : IRabbitMQConnectionProvider
    {
        public string Host => "localhost";
        public string VirtualHost => "myvhost";
        public int Port => 5672;

        public ValueTask<global::RabbitMQ.Client.IConnection> CreateAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private static MessagingRuntime CreateRuntime(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}
