using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Faults.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class FaultEndpointTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task FaultEndpoint_Should_ReplaceTheDerivedSubjects_When_AddressesAreGiven()
    {
        // arrange
        // Fault and skipped subjects are derived from the endpoint name, which for some endpoints the
        // framework names after a shared message type with no service scoping. Overriding the
        // addresses is what lets two services keep their fault traffic apart.
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<ChargeHandler>()
            .AddNats(nats => nats
                .StreamName("fault-service")
                .Endpoint("charges")
                .Handler<ChargeHandler>()
                .FaultEndpoint(new Uri("nats:s/fault-service.charges_error"))
                .SkippedEndpoint(new Uri("nats:s/fault-service.charges_skipped")));

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // assert
            // The overridden subjects are captured, and the derived 'charges_error' pair is absent.
            Snapshot.Create()
                .Add(string.Join(
                    "\n",
                    Assert.Single(topology.Streams).Subjects.Order(StringComparer.Ordinal)))
                .MatchMarkdown();
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task DisableFaultEndpoint_Should_OmitTheSubject_When_FaultsAreNotForwarded()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<RefundHandler>()
            .AddNats(nats => nats
                .StreamName("nofault-service")
                .Endpoint("refunds")
                .Handler<RefundHandler>()
                .DisableFaultEndpoint()
                .DisableSkippedEndpoint());

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // assert
            // Only the consumed subject remains; nothing is provisioned for faults.
            Assert.Equal(
                ["mocha.faults.contracts.refund-attempted"],
                Assert.Single(topology.Streams).Subjects.Order(StringComparer.Ordinal));
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Theory]
    [InlineData("s/orders_error")]
    [InlineData("amqp://localhost/e/orders_error")]
    public void FaultEndpoint_Should_Throw_When_TheAddressIsNotASubject(string address)
    {
        // arrange
        var descriptor = new NatsMessagingTransportDescriptor(Moq.Mock.Of<IMessagingSetupContext>());
        var endpoint = descriptor.Endpoint("orders");

        // act
        var exception = Assert.Throws<ArgumentException>(
            () => endpoint.FaultEndpoint(new Uri(address, UriKind.RelativeOrAbsolute)));

        // assert
        Assert.Equal("address", exception.ParamName);
    }

    public sealed class ChargeHandler : IEventHandler<ChargeAttempted>
    {
        public ValueTask HandleAsync(ChargeAttempted message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    public sealed class RefundHandler : IEventHandler<RefundAttempted>
    {
        public ValueTask HandleAsync(RefundAttempted message, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
