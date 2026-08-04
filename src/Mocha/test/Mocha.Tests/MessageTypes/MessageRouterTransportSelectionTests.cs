using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for <see cref="MessageRouter.GetEndpoint"/> transport selection logic,
/// covering the deterministic routing rules defined in W3: flagged default, sole transport,
/// first transport fallback, and scheme-qualified destination.
/// </summary>
public class MessageRouterTransportSelectionTests
{
    [Fact]
    public void GetEndpoint_Should_RouteToFlaggedTransport_When_OneIsDefaultTransport()
    {
        // arrange
        // Two transports registered; only "alpha" is flagged as the default.
        // GetEndpoint creates an on-the-fly outbound route (no pre-registration), so
        // MessagingTransportSelection.Select is called and must resolve to "alpha".
        var runtime = CreateRuntime(b => b
            .AddInMemory(t => t.Name("alpha").Schema("alpha").IsDefaultTransport())
            .AddInMemory(t => t.Name("beta").Schema("beta")));
        var messageType = runtime.GetMessageType(typeof(DefaultFlaggedEvent));

        // act
        var endpoint = runtime.GetPublishEndpoint(messageType);

        // assert
        Assert.Equal("alpha", endpoint.Transport.Name);
    }

    [Fact]
    public void GetEndpoint_Should_RouteToSoleTransport_When_NoDefaultFlaggedAndSingleTransport()
    {
        // arrange
        // Single transport with no IsDefaultTransport flag; sole-transport fallback applies.
        var runtime = CreateRuntime(b => b.AddInMemory(t => t.Name("only").Schema("only")));
        var messageType = runtime.GetMessageType(typeof(SoleTransportEvent));

        // act
        var endpoint = runtime.GetPublishEndpoint(messageType);

        // assert
        Assert.Equal("only", endpoint.Transport.Name);
    }

    [Fact]
    public void GetEndpoint_Should_RouteToFirstTransport_When_NoDefaultAndMultipleTransports()
    {
        // arrange
        // Two transports with no default; implicit routing falls back to the first registered transport.
        var runtime = CreateRuntime(b => b
            .AddInMemory(t => t.Name("alpha").Schema("alpha"))
            .AddInMemory(t => t.Name("beta").Schema("beta")));
        var messageType = runtime.GetMessageType(typeof(AmbiguousEvent));

        // act
        var endpoint = runtime.GetPublishEndpoint(messageType);

        // assert
        Assert.Equal("alpha", endpoint.Transport.Name);
    }

    [Fact]
    public void SchemeQualifiedDestination_Should_RouteByScheme_When_TransportSchemeUsed()
    {
        // arrange
        // Route destination uses the "beta" transport's URI schema ("beta:q/<name>").
        // At build time EndpointRouter.GetOrCreate delegates to the "beta" transport,
        // so the resolved endpoint belongs to "beta" even though "alpha" is the default.
        var runtime = CreateRuntime(b => b
            .AddMessage<SchemeEvent>(m => m.Publish(r => r.Destination(new Uri("beta:q/scheme-queue"))))
            .AddInMemory(t => t.Name("alpha").Schema("alpha").IsDefaultTransport())
            .AddInMemory(t => t.Name("beta").Schema("beta")));
        var messageType = runtime.Messages.MessageTypes.Single(mt => mt.RuntimeType == typeof(SchemeEvent));

        // act
        var endpoint = runtime.GetPublishEndpoint(messageType);

        // assert
        Assert.Equal("beta", endpoint.Transport.Name);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    // Distinct message types per test to avoid routing-table collisions.
    public sealed class DefaultFlaggedEvent;
    public sealed class SoleTransportEvent;
    public sealed class AmbiguousEvent;
    public sealed class SchemeEvent;
}
