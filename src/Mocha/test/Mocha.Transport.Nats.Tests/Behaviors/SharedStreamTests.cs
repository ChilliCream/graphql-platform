using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Ops.Contracts;
using Mocha.Shared.Contracts;
using Mocha.Transport.Nats.Tests.Fixtures;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class SharedStreamTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task StartAsync_Should_BindToTheOwningStream_When_AnotherServiceAlreadyCapturesTheSubject()
    {
        // arrange
        // Convention subjects come from the message type, so both services derive the same subject,
        // and JetStream requires stream subjects to be disjoint. The second service has to bind to
        // the stream that already owns the subject rather than declaring a competing one.
        var billing = new MessageRecorder();
        var analytics = new MessageRecorder();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var billingHost = BuildHost<BillingHandler>("shared-billing", billing);
        using var analyticsHost = BuildHost<AnalyticsHandler>("shared-analytics", analytics);

        // act
        await billingHost.StartAsync(cancellationToken);
        await analyticsHost.StartAsync(cancellationToken);

        try
        {
            await billingHost.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new WidgetShipped("W-1"), cancellationToken);

            // assert
            Assert.True(await billing.WaitAsync(s_timeout), "The first service did not receive the event.");
            Assert.True(await analytics.WaitAsync(s_timeout), "The second service did not receive the event.");

            new Snapshot()
                .Add(Describe(billingHost), "FirstService")
                .Add(Describe(analyticsHost), "SecondService")
                .MatchMarkdown();
        }
        finally
        {
            await analyticsHost.StopAsync(cancellationToken);
            await billingHost.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task StartAsync_Should_ReuseTheExistingStream_When_ItWasProvisionedOutsideTheTransport()
    {
        // arrange
        // Ops-managed topology: the stream exists before any service starts, which is the documented
        // production path.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        await fixture.JetStream.CreateOrUpdateStreamAsync(
            new StreamConfig
            {
                Name = "OPS_OWNED",
                Subjects = ["mocha.ops.contracts.>"],
                Storage = StreamConfigStorage.Memory
            },
            cancellationToken);

        using var host = BuildHost<OpsHandler>("shared-ops", recorder);

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new InvoiceRaised("INV-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the event.");
            Assert.Equal("OPS_OWNED", ConsumerFor(host, "mocha.ops.contracts.invoice-raised").StreamName);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task StartAsync_Should_StillCoverItsOwnSubjects_When_ItDeclaresAnotherServicesStream()
    {
        // arrange
        // The documented recipe for removing the start-up ordering dependency: a subscriber declares
        // the stream it consumes from. That must not leave the subscriber's own published subjects
        // uncaptured, which previously failed start-up outright.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<DeclaredStreamHandler>()
            .AddNats(nats => nats
                .StreamName("shared-declaring")
                .DeclareStream("DECLARED_UPSTREAM")
                .Subject("mocha.declared.contracts.>")
                .Storage(StreamConfigStorage.Memory));

        using var host = builder.Build();

        // act
        await host.StartAsync(cancellationToken);

        try
        {
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new PalletLoaded("P-1"), cancellationToken);

            // assert
            Assert.True(await recorder.WaitAsync(s_timeout), "The handler did not receive the event.");

            new Snapshot()
                .Add(Describe(host), "Topology")
                .MatchMarkdown();
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Renders which stream each service declared and which stream its consumers read from.
    /// </summary>
    private static string Describe(IHost host)
    {
        var topology = Topology(host);

        var streams = topology.Streams
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => $"stream {s.Name} captures [{string.Join(", ", s.Subjects.Order(StringComparer.Ordinal))}]");

        var consumers = topology.Consumers
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => $"consumer {c.Name} reads {c.StreamName} "
                + $"filtered on [{string.Join(", ", c.FilterSubjects.Order(StringComparer.Ordinal))}]");

        return string.Join("\n", streams.Concat(consumers));
    }

    private static NatsMessagingTopology Topology(IHost host)
        => (NatsMessagingTopology)host.Services
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<NatsMessagingTransport>()
            .Single()
            .Topology;

    private static NatsConsumer ConsumerFor(IHost host, string subject)
        => Topology(host).Consumers.Single(c => c.FilterSubjects.Contains(subject));

    private IHost BuildHost<THandler>(string serviceName, MessageRecorder recorder)
        where THandler : class, IEventHandler
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<THandler>()
            .AddNats(nats => nats.StreamName(serviceName));

        return builder.Build();
    }

    public sealed class BillingHandler(MessageRecorder recorder) : IEventHandler<WidgetShipped>
    {
        public ValueTask HandleAsync(WidgetShipped message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class AnalyticsHandler(MessageRecorder recorder) : IEventHandler<WidgetShipped>
    {
        public ValueTask HandleAsync(WidgetShipped message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class OpsHandler(MessageRecorder recorder) : IEventHandler<InvoiceRaised>
    {
        public ValueTask HandleAsync(InvoiceRaised message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class DeclaredStreamHandler(MessageRecorder recorder) : IEventHandler<PalletLoaded>
    {
        public ValueTask HandleAsync(PalletLoaded message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
