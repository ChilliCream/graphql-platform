using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

/// <summary>
/// Tests for the eager configuration-level invariants validated in <see cref="MessageBusBuilder.Build"/>.
/// Covers the three errors that can be detected at build time before any dispatch runs:
/// multiple default-flagged transports, an OnTransport reference to an unknown transport name,
/// and an OnTransport override that conflicts with a scheme-qualified destination.
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

    [Fact]
    public void Build_Should_Fail_When_OnTransportTargetsUnknownName()
    {
        // arrange
        // A message type routes via OnTransport("missing") but no transport with that name
        // is registered; Build() must throw before topology discovery.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddMessage<UnknownTransportTargetEvent>(m => m.Publish(r => r.OnTransport("missing")))
            .AddInMemory(t => t.Name("alpha").Schema("alpha"));

        var provider = services.BuildServiceProvider();

        // act
        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IMessagingRuntime>());

        // assert
        ex.Message.MatchSnapshot();
    }

    [Fact]
    public void Build_Should_Fail_When_OnTransportConflictsWithDestinationScheme()
    {
        // arrange
        // A message type has both OnTransport("alpha") and a destination URI whose scheme
        // matches the "beta" transport; these two selectors disagree, so Build() must throw.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder
            .AddMessage<SchemeConflictEvent>(m => m.Publish(r => r
                .OnTransport("alpha")
                .Destination(new Uri("beta:q/some-queue"))))
            .AddInMemory(t => t.Name("alpha").Schema("alpha").IsDefaultTransport())
            .AddInMemory(t => t.Name("beta").Schema("beta"));

        var provider = services.BuildServiceProvider();

        // act
        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<IMessagingRuntime>());

        // assert
        ex.Message.MatchSnapshot();
    }

    // Distinct message types per test to avoid routing-table collisions.
    public sealed class UnknownTransportTargetEvent;
    public sealed class SchemeConflictEvent;
}
