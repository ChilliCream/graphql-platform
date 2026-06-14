using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for the eager configuration-level invariants validated in <see cref="MessageBusBuilder.Build"/>.
/// Covers the errors that can be detected at build time before any dispatch runs:
/// multiple default-flagged transports.
/// </summary>
public sealed class MessageBusBuilderTransportValidationTests
{
    [Fact]
    public void Build_Should_Fail_When_MultipleTransportsFlaggedDefault()
    {
        // arrange
        // Two transports both marked IsDefaultTransport; Build() must detect this
        // configuration error before any topology or endpoint discovery runs.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddInMemory(t => t.Name("alpha").Schema("alpha").IsDefaultTransport())
            .AddInMemory(t => t.Name("beta").Schema("beta").IsDefaultTransport());

        var provider = services.BuildServiceProvider();

        // act
        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IMessagingRuntime>());

        // assert
        ex.Message.MatchSnapshot();
    }
}
