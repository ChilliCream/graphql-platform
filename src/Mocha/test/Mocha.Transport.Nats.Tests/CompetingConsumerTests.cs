using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record WorkItem(int Sequence);

[Collection(JetStreamCollection.Name)]
public class CompetingConsumerTests(JetStreamFixture fixture)
{
    private const int MessageCount = 20;

    [Fact]
    public async Task StartAsync_Should_ShareOneDurable_When_TwoInstancesOfAServiceRun()
    {
        // arrange
        // Competing consumption on JetStream is two instances pulling from the same durable, so the
        // durable name has to be derived identically on both rather than per instance, and each
        // message must still be handled exactly once.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        using var first = BuildInstance(recorder);
        using var second = BuildInstance(recorder);

        await first.StartAsync(cancellationToken);
        await second.StartAsync(cancellationToken);

        try
        {
            Assert.Equal(ConsumerNames(first), ConsumerNames(second));

            var bus = first.Services.GetRequiredService<IMessageBus>();

            // act
            for (var i = 0; i < MessageCount; i++)
            {
                await bus.PublishAsync(new WorkItem(i), cancellationToken);
            }

            Assert.True(
                await recorder.WaitAsync(TimeSpan.FromSeconds(60), expectedCount: MessageCount),
                $"Only {recorder.Messages.Count} of {MessageCount} messages were handled in time.");

            // Give a duplicate delivery the chance to arrive before claiming exactly-once.
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            // assert
            Assert.Equal(
                Enumerable.Range(0, MessageCount),
                recorder.Messages.Cast<WorkItem>().Select(w => w.Sequence).Order());
        }
        finally
        {
            await second.StopAsync(cancellationToken);
            await first.StopAsync(cancellationToken);
        }
    }

    private IHost BuildInstance(MessageRecorder recorder)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<WorkItemHandler>()
            .AddNats(nats => nats.StreamName("e2e-workers"));

        return builder.Build();
    }

    private static List<string> ConsumerNames(IHost host)
        => [.. ((NatsMessagingTopology)host.Services
            .GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<NatsMessagingTransport>()
            .Single()
            .Topology)
            .Consumers
            .Select(c => c.Name)
            .Order(StringComparer.Ordinal)];

    public sealed class WorkItemHandler(MessageRecorder recorder) : IEventHandler<WorkItem>
    {
        public ValueTask HandleAsync(WorkItem message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return ValueTask.CompletedTask;
        }
    }
}
