using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Sagas;
using Mocha.Transport.Nats.Tests.Fixtures;
using Timeouts.Contracts;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class SagaTimeoutScopingTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task Endpoint_Should_ScopeTheSagaTimeout_When_TwoServicesHostSagas()
    {
        // arrange
        // Only Subscribe routes are host-scoped, so every saga-hosting service derives the same bare
        // 'saga-timed-out' endpoint. Declaring it under that name folds in and scopes it per service.
        var cancellationToken = TestContext.Current.CancellationToken;

        using var alpha = BuildHost("alpha-service");
        using var beta = BuildHost("beta-service");

        // act
        await alpha.StartAsync(cancellationToken);
        await beta.StartAsync(cancellationToken);

        try
        {
            // assert
            Snapshot.Create()
                .Add(Describe(alpha), "AlphaService")
                .Add(Describe(beta), "BetaService")
                .MatchMarkdown();
        }
        finally
        {
            await beta.StopAsync(cancellationToken);
            await alpha.StopAsync(cancellationToken);
        }
    }

    private static string Describe(IHost host)
    {
        var topology = (NatsMessagingTopology)host.Services
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<NatsMessagingTransport>()
            .Single()
            .Topology;

        var timeoutSubjects = topology.Subjects
            .Where(s => s.Subject.Contains("timed-out", StringComparison.Ordinal))
            .Select(s => s.Subject)
            .Order(StringComparer.Ordinal);

        var timeoutConsumers = topology.Consumers
            .Where(c => c.Name.Contains("timed-out", StringComparison.Ordinal))
            .Select(c => c.Name)
            .Order(StringComparer.Ordinal);

        return string.Join(
            "\n",
            $"timeout durables: {string.Join(", ", timeoutConsumers)}",
            $"timeout fault subjects: {string.Join(", ", timeoutSubjects)}");
    }

    private IHost BuildHost(string serviceName)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddInMemorySagas();

        var bus = builder.Services.AddMessageBus();

        bus.ConfigureMessageBus(mocha => mocha.Host(host => host.ServiceName(serviceName)));
        bus.ConfigureMessageBus(mocha => mocha.AddSaga<TimeoutScopingSaga>());

        bus.AddNats(nats =>
        {
            nats.StreamName(serviceName);

            // Named exactly as the framework derives it, so this configures that endpoint rather
            // than adding a second one.
            nats.Endpoint("saga-timed-out")
                .ConsumerName($"{serviceName}_saga-timed-out")
                .FaultEndpoint(new Uri($"nats:s/{serviceName}.saga-timed-out_error"))
                .SkippedEndpoint(new Uri($"nats:s/{serviceName}.saga-timed-out_skipped"));
        });

        return builder.Build();
    }

    public sealed class TimeoutScopingState : SagaStateBase;

    public sealed class TimeoutScopingSaga : Saga<TimeoutScopingState>
    {
        protected override void Configure(ISagaDescriptor<TimeoutScopingState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<ScopingStarted>()
                .StateFactory(_ => new TimeoutScopingState())
                .TransitionTo("Active");

            descriptor.During("Active").OnTimeout().TransitionTo("TimedOut");

            descriptor.Finally("TimedOut");
        }
    }
}
