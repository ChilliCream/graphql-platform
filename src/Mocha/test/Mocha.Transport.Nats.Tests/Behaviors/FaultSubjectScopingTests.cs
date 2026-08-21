using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class FaultSubjectScopingTests(JetStreamFixture fixture)
{
    private const string HostServiceName = "fault-scope-service";

    [Fact]
    public async Task FaultSubjects_Should_BeScopedToTheService_When_TheEndpointIsNamedExplicitly()
    {
        // arrange
        // A name the caller chose carries no namespace of its own, and stream subjects have to be
        // disjoint, so an unscoped fault subject is claimed by whichever service provisions first.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildAsync(scope, endpointName: "order-commands");

        // act
        var faults = FaultSubjects(bus);

        // assert
        Assert.Equal(
            [
                $"{HostServiceName}.order-commands_error",
                $"{HostServiceName}.order-commands_skipped"
            ],
            faults);
    }

    [Fact]
    public async Task FaultSubjects_Should_KeepTheDerivedScope_When_TheEndpointIsConventionallyNamed()
    {
        // arrange
        // A handler-derived endpoint name already begins with the service, so nothing is prefixed twice.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildAsync(scope, endpointName: null);

        // act
        var faults = FaultSubjects(bus);

        // assert
        Assert.Equal(
            [
                $"{HostServiceName}.order-created_error",
                $"{HostServiceName}.order-created_skipped"
            ],
            faults);
    }

    [Fact]
    public async Task FaultSubjects_Should_NotBePrefixed_When_TheEndpointNameIsAlreadyNamespaced()
    {
        // arrange
        // A caller who namespaced the endpoint keeps that namespace rather than having the service
        // prepended to it.
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await BuildAsync(scope, endpointName: "contracts.orders.commands");

        // act
        var faults = FaultSubjects(bus);

        // assert
        Assert.Equal(
            ["contracts.orders.commands_error", "contracts.orders.commands_skipped"],
            faults);
    }

    private Task<TestBus> BuildAsync(JetStreamScope scope, string? endpointName)
    {
        var services = new ServiceCollection();
        services.AddSingleton(fixture.Connection);
        services.AddSingleton(new MessageRecorder());

        var bus = services.AddMessageBus();

        // Pinned rather than inherited, since the fallback is the test assembly name.
        bus.ConfigureMessageBus(mocha => mocha.Host(host => host.ServiceName(HostServiceName)));

        return bus
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(nats =>
            {
                nats.StreamName(scope.StreamName);

                if (endpointName is not null)
                {
                    nats.Endpoint(endpointName).Handler<OrderCreatedHandler>();
                }
            })
            .BuildTestBusAsync();
    }

    private static List<string> FaultSubjects(TestBus bus)
        =>
        [
            .. bus.Topology.Subjects
                .Select(s => s.Subject)
                .Where(s =>
                    s.EndsWith("_error", StringComparison.Ordinal)
                    || s.EndsWith("_skipped", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
        ];
}
