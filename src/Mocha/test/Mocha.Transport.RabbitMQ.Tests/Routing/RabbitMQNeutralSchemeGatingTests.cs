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
        // queue: is a neutral scheme supported by RabbitMQ transports.
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
    public void NeutralScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // Two RabbitMQ transports; the one under test is not the default. It still advertises
        // capability, while EndpointRouter decides whether this candidate is selected.
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
        Assert.NotNull(configuration);
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
    public void QueueAndExchangeScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // A non-default RabbitMQ transport still supports queue: and exchange: URIs.
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
        Assert.NotNull(queueConfig);
        Assert.NotNull(exchangeConfig);
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
