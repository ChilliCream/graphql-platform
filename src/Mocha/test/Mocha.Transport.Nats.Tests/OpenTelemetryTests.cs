using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record TracedEvent(Guid Id);

[Collection(JetStreamCollection.Name)]
public class OpenTelemetryTests(JetStreamFixture fixture)
{
    [Fact]
    public async Task PublishAsync_Should_EmitOnlyNatsClientSpans_When_AnEventIsHandled()
    {
        // arrange
        // The NATS client always registers its own ActivitySource, so this fails if Mocha ever starts
        // tracing here too, rather than silently shipping duplicate spans.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();
        var activities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };

        ActivitySource.AddActivityListener(listener);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<TracedEventHandler>()
            .AddNats(nats => nats.StreamName("otel-service"));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new TracedEvent(Guid.NewGuid()), cancellationToken);

            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
                "The handler did not receive the event.");
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }

        // assert
        var mochaSources = activities
            .Select(a => a.Source.Name)
            .Where(name => name.Contains("Mocha", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Contains("NATS.Net", activities.Select(a => a.Source.Name));
        Assert.Equal([], mochaSources);
    }

    public sealed class TracedEventHandler(MessageRecorder recorder) : IEventHandler<TracedEvent>
    {
        public ValueTask HandleAsync(TracedEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
