using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Routing;

/// <summary>
/// Verifies that <see cref="InMemoryMessagingTransport.CreateEndpointConfiguration(IMessagingConfigurationContext, Uri)"/>
/// only claims neutral-scheme URIs (<c>queue:</c> and <c>topic:</c>) when the transport is the
/// effective default (flagged with <see cref="IInMemoryMessagingTransportDescriptor.IsDefaultTransport"/>
/// or sole transport registered), and returns <see langword="null"/> otherwise.
/// </summary>
public class InMemoryNeutralSchemeGatingTests
{
    [Fact]
    public void NeutralScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // Two in-memory transports; the one under test carries IsDefaultTransport().
        // queue: is the cross-transport neutral scheme; the default transport must claim it.
        var runtime = CreateRuntime(b => b
            .AddInMemory(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
            })
            .AddInMemory(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
            }));
        var primary = runtime.Transports.OfType<InMemoryMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var configuration = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.NotNull(configuration);
    }

    [Fact]
    public void NeutralScheme_Should_NotBeClaimed_When_TransportIsNotDefault()
    {
        // arrange
        // Two in-memory transports; the one under test is NOT the default.
        // A non-default transport must not claim queue: URIs; it would silently absorb
        // messages the caller intended for the default transport.
        var runtime = CreateRuntime(b => b
            .AddInMemory(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
            })
            .AddInMemory(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
            }));
        var secondary = runtime.Transports.OfType<InMemoryMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var configuration = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));

        // assert
        Assert.Null(configuration);
    }

    [Fact]
    public void QueueAndTopicScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // The default in-memory transport claims both queue: and topic: neutral schemes.
        var runtime = CreateRuntime(b => b
            .AddInMemory(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
            })
            .AddInMemory(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
            }));
        var primary = runtime.Transports.OfType<InMemoryMessagingTransport>().Single(t => t.Name == "primary");

        // act
        var queueConfig = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var topicConfig = primary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.NotNull(queueConfig);
        Assert.NotNull(topicConfig);
    }

    [Fact]
    public void QueueAndTopicScheme_Should_NotBeClaimed_When_TransportIsNotDefault()
    {
        // arrange
        // A non-default in-memory transport must not claim queue: or topic: URIs.
        var runtime = CreateRuntime(b => b
            .AddInMemory(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
            })
            .AddInMemory(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
            }));
        var secondary = runtime.Transports.OfType<InMemoryMessagingTransport>().Single(t => t.Name == "secondary");

        // act
        var queueConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var topicConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.Null(queueConfig);
        Assert.Null(topicConfig);
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
