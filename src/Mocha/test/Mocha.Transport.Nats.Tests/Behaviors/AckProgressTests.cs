using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

public sealed record LongRunningJob(string JobId);

[Collection(JetStreamCollection.Name)]
public class AckProgressTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_ackWait = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan s_handlerDuration = TimeSpan.FromSeconds(8);

    [Fact]
    public async Task AckProgressEvery_Should_PreventRedelivery_When_TheHandlerOutlivesAckWait()
    {
        // arrange
        // The handler runs for well over AckWait. Without progress reporting the deadline would
        // expire mid-handler and JetStream would deliver the message again, so a single delivery is
        // what proves NatsAcknowledgementMiddleware is extending it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddEventHandler<LongRunningJobHandler>()
            .AddNats(nats =>
            {
                nats.StreamName("ack-progress")
                    .Endpoint("slow-job")
                    .Handler<LongRunningJobHandler>();

                nats.DeclareConsumer("slow-job")
                    .AckWait(s_ackWait)
                    .AckProgressEvery(TimeSpan.FromSeconds(1));
            });

        using var host = builder.Build();
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            await host.Services.GetRequiredService<IMessageBus>()
                .PublishAsync(new LongRunningJob("J-1"), cancellationToken);

            Assert.True(
                await recorder.WaitAsync(s_handlerDuration + TimeSpan.FromSeconds(30)),
                "The handler never completed.");

            // Give a redelivery the chance to arrive before claiming there was only one.
            await Task.Delay(s_ackWait + TimeSpan.FromSeconds(2), cancellationToken);

            // assert
            Assert.Equal(
                [new LongRunningJob("J-1")],
                recorder.Messages.Cast<LongRunningJob>().ToList());
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    public sealed class LongRunningJobHandler(MessageRecorder recorder) : IEventHandler<LongRunningJob>
    {
        public async ValueTask HandleAsync(LongRunningJob message, CancellationToken cancellationToken)
        {
            await Task.Delay(s_handlerDuration, cancellationToken);

            recorder.Record(message);
        }
    }
}
