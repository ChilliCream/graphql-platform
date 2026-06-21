using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Verifies the end-to-end neutral-scheme claim and lazy-dispatch-failure rules.
/// Neutral schemes (<c>queue:</c> and <c>topic:</c>) resolve lazily through
/// <see cref="IMessagingRuntime.GetDispatchEndpoint"/>. When multiple transports can handle the
/// same neutral address and none is default, dispatch fails with an ambiguity diagnostic.
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
    public void Resolve_Should_ThrowAmbiguous_When_NeutralSchemeMatchesMultipleTransportsAndNoDefault()
    {
        // arrange
        // Two in-memory transports with no default flag set.
        // Build succeeds: neutral-scheme resolution is not validated at build time.
        // The ambiguity surfaces lazily when the address is first dispatched.
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
            "Multiple transports can handle address: queue:order-commands. Matching transports: 'alpha', 'beta'. Mark one as default or use a transport-specific address.");
    }
}
