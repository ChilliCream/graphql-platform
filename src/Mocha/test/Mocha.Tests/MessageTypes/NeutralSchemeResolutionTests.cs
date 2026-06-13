using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Verifies the end-to-end neutral-scheme claim and lazy-dispatch-failure rules (D11, W3.13).
/// Neutral schemes (<c>queue:</c> and <c>topic:</c>) are exclusive to the transport flagged as
/// the effective default. When no default exists and a neutral-scheme address is used, the error
/// surfaces at dispatch time via <see cref="IMessagingRuntime.GetDispatchEndpoint"/>, not at build
/// time, because the claim depends on runtime transport selection.
/// </summary>
public sealed class NeutralSchemeResolutionTests
{
    [Fact]
    public void Resolve_Should_ClaimNeutralSchemeOnDefaultOnly_When_TwoTransportsRegistered()
    {
        // arrange
        // Two in-memory transports; only "alpha" is flagged as the default.
        // The queue: neutral scheme is only claimed by the effective default transport.
        // Resolving a queue: address must therefore return an endpoint owned by "alpha".
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
    public void Resolve_Should_Throw_When_NeutralSchemeAndNoDefault()
    {
        // arrange
        // Two in-memory transports with no default flag set.
        // Neither transport claims queue: URIs because neither is the effective default.
        // Build succeeds: neutral-scheme resolution is not validated at build time.
        // The failure surfaces lazily when the address is first dispatched.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t => t.Name("alpha").Schema("alpha"))
            .AddInMemory(t => t.Name("beta").Schema("beta"));

        var provider = services.BuildServiceProvider();

        // act
        // Build completes without error; the ambiguity is lazy.
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();

        var ex = Assert.Throws<InvalidOperationException>(
            () => runtime.GetDispatchEndpoint(new Uri("queue:order-commands")));

        // assert
        ex.Message.MatchInlineSnapshot(
            "No transport can handle address: queue:order-commands");
    }
}
