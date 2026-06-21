using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Routing;

/// <summary>
/// Verifies that <see cref="InMemoryMessagingTransport.CreateEndpointConfiguration(IMessagingConfigurationContext, Uri)"/>
/// can describe supported neutral-scheme URIs (<c>queue:</c> and <c>topic:</c>). The central
/// endpoint router applies the cross-transport selection rules.
/// </summary>
public class InMemoryNeutralSchemeGatingTests
{
    [Fact]
    public void NeutralScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // Two in-memory transports; the one under test carries IsDefaultTransport().
        // queue: is a neutral scheme supported by in-memory transports.
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
    public void NeutralScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // Two in-memory transports; the one under test is not the default. It still advertises
        // capability, while EndpointRouter decides whether this candidate is selected.
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
        Assert.NotNull(configuration);
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
    public void QueueAndTopicScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // A non-default in-memory transport still supports queue: and topic: URIs.
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
        Assert.NotNull(queueConfig);
        Assert.NotNull(topicConfig);
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
