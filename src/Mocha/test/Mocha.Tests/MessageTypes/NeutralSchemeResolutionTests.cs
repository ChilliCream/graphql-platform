using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Verifies the end-to-end neutral-scheme claim and lazy-dispatch-failure rules.
/// Neutral schemes (<c>queue:</c> and <c>topic:</c>) resolve lazily through
/// <see cref="IMessagingRuntime.GetDispatchEndpoint"/>. When multiple transports can handle the
/// same neutral address and none is default, the first candidate is selected.
/// </summary>
public sealed class NeutralSchemeResolutionTests
{
    [Fact]
    public void Resolve_Should_ClaimNeutralSchemeOnDefaultOnly_When_TwoTransportsRegistered()
    {
        // arrange
        // Two in-memory transports; only "alpha" is flagged as the default.
        // Resolving a queue: address must prefer the default transport.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t => t.Name("alpha").Schema("alpha").IsDefaultTransport())
            .AddInMemory(t => t.Name("beta").Schema("beta"));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("queue:order-commands"));

        // assert
        Assert.Equal("alpha", endpoint.Transport.Name);
    }

    [Fact]
    public void Resolve_Should_UseDefaultTransport_When_DefaultRegisteredAfterMatchingTransport()
    {
        // arrange
        // The first transport can handle queue:, but the second one is flagged as default.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t => t.Name("alpha").Schema("alpha"))
            .AddInMemory(t => t.Name("beta").Schema("beta").IsDefaultTransport());

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("queue:order-commands"));

        // assert
        Assert.Equal("beta", endpoint.Transport.Name);
    }

    [Fact]
    public void Resolve_Should_UseExistingEndpoint_When_NonDefaultTransportAlreadyHasEndpoint()
    {
        // arrange
        // An existing endpoint is already bound to the queue on the first transport.
        // The default transport only matters when the endpoint must be created lazily.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t =>
            {
                t.Name("alpha").Schema("alpha");
                t.DispatchEndpoint("orders").ToQueue("order-commands");
            })
            .AddInMemory(t => t.Name("beta").Schema("beta").IsDefaultTransport());

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("queue:order-commands"));

        // assert
        Assert.Equal("alpha", endpoint.Transport.Name);
    }

    [Fact]
    public void Resolve_Should_UseFirstMatchingTransport_When_NeutralSchemeMatchesMultipleTransportsAndNoDefault()
    {
        // arrange
        // Two in-memory transports with no default flag set.
        // Build succeeds, and dispatch falls back to the first matching transport.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t => t.Name("alpha").Schema("alpha"))
            .AddInMemory(t => t.Name("beta").Schema("beta"));

        var provider = services.BuildServiceProvider();

        // act
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var endpoint = runtime.GetDispatchEndpoint(new Uri("queue:order-commands"));

        // assert
        Assert.Equal("alpha", endpoint.Transport.Name);
    }
}
