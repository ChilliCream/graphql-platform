using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

public class RabbitMQNeutralSchemeGatingTests
{
    [Fact]
    public void NeutralScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // Two RabbitMQ transports; the one under test carries IsDefaultTransport().
        // queue: is the cross-transport neutral scheme; the default transport must claim it.
        var runtime = CreateRuntime(b => b
            .AddRabbitMQ(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionProvider(_ => new StubConnectionProvider());
            })
            .AddRabbitMQ(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            }));
        var primary = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var configuration = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void NeutralScheme_Should_NotBeClaimed_When_TransportIsNotDefault()
    {
        // arrange
        // Two RabbitMQ transports; the one under test is NOT the default.
        // A non-default transport must not claim queue: URIs; it would route messages to
        // the wrong broker when the caller intended to address the default transport.
        var runtime = CreateRuntime(b => b
            .AddRabbitMQ(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionProvider(_ => new StubConnectionProvider());
            })
            .AddRabbitMQ(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            }));
        var secondary = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var configuration = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.Null(configuration);
    }

    [Fact]
    public void QueueAndExchangeScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // The default RabbitMQ transport claims both queue: and exchange: neutral schemes.
        var runtime = CreateRuntime(b => b
            .AddRabbitMQ(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionProvider(_ => new StubConnectionProvider());
            })
            .AddRabbitMQ(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            }));
        var primary = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var queueConfig = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var exchangeConfig = primary.CreateEndpointConfiguration(runtime, new Uri("exchange:orders"));

        // assert
        Assert.NotNull(queueConfig);
        Assert.NotNull(exchangeConfig);
    }

    [Fact]
    public void QueueAndExchangeScheme_Should_NotBeClaimed_When_TransportIsNotDefault()
    {
        // arrange
        // A non-default RabbitMQ transport must not claim queue: or exchange: URIs.
        var runtime = CreateRuntime(b => b
            .AddRabbitMQ(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionProvider(_ => new StubConnectionProvider());
            })
            .AddRabbitMQ(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionProvider(_ => new StubConnectionProvider());
            }));
        var secondary = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var queueConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var exchangeConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("exchange:orders"));

        // assert
        Assert.Null(queueConfig);
        Assert.Null(exchangeConfig);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder.BuildRuntime();
    }
}
