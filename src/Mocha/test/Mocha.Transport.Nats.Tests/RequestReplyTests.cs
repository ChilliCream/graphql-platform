using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.Transport.Nats.Tests.Fixtures;
using Xunit;

namespace Mocha.Transport.Nats.Tests;

public sealed record ProcessRefund(Guid RefundId, decimal Amount) : IEventRequest<RefundProcessed>;

public sealed record RefundProcessed(Guid RefundId, string Status);

public sealed record InspectRefund(Guid RefundId) : IEventRequest<RefundInspected>;

public sealed record RefundInspected(Guid RefundId, string Status);

[Collection(JetStreamCollection.Name)]
public class RequestReplyTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task RequestAsync_Should_ReturnTheResponse_When_HandledOverACoreNatsReply()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();
        var refundId = Guid.NewGuid();

        using var host = BuildHost<ProcessRefundHandler>("e2e-refund-roundtrip", recorder);
        await host.StartAsync(cancellationToken);

        try
        {
            // act
            var response = await host.Services.GetRequiredService<IMessageBus>()
                .RequestAsync(new ProcessRefund(refundId, 49.99m), cancellationToken)
                .AsTask()
                .WaitAsync(s_timeout, cancellationToken);

            // assert
            // Separated from the response so a failure says which half broke: the request never
            // reaching the handler, or the reply never getting back over the core subscription.
            Assert.Equal(new ProcessRefund(refundId, 49.99m), Assert.Single(recorder.Messages));
            Assert.Equal(new RefundProcessed(refundId, "refunded"), response);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task DiscoverTopology_Should_KeepReplySubjectsOutOfEveryStream_When_RequestHandlerRegistered()
    {
        // arrange
        // Reply inboxes are ephemeral. Capturing one in a stream would persist every response for
        // the stream's whole retention period.
        var cancellationToken = TestContext.Current.CancellationToken;
        var recorder = new MessageRecorder();

        using var host = BuildHost<InspectRefundHandler>("e2e-refund-topology", recorder);
        await host.StartAsync(cancellationToken);

        try
        {
            var topology = (NatsMessagingTopology)host.Services
                .GetRequiredService<IMessagingRuntime>()
                .Transports.OfType<NatsMessagingTransport>()
                .Single()
                .Topology;

            // act
            var replySubjects = topology.Subjects.Where(s => s.IsCore).Select(s => s.Subject).ToList();

            var captured = replySubjects
                .Where(subject => topology.Streams.Any(stream => stream.Subjects.Contains(subject)))
                .ToList();

            // assert
            Assert.NotEmpty(replySubjects);
            Assert.Equal([], captured);
        }
        finally
        {
            await host.StopAsync(cancellationToken);
        }
    }

    private IHost BuildHost<THandler>(string serviceName, MessageRecorder recorder)
        where THandler : class, IEventRequestHandler
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton(fixture.Connection);
        builder.Services.AddSingleton(recorder);
        builder.Services
            .AddMessageBus()
            .AddRequestHandler<THandler>()
            .AddNats(nats => nats.StreamName(serviceName));

        return builder.Build();
    }

    public sealed class ProcessRefundHandler(MessageRecorder recorder)
        : IEventRequestHandler<ProcessRefund, RefundProcessed>
    {
        public ValueTask<RefundProcessed> HandleAsync(
            ProcessRefund message,
            CancellationToken cancellationToken)
        {
            recorder.Record(message);

            return ValueTask.FromResult(new RefundProcessed(message.RefundId, "refunded"));
        }
    }

    public sealed class InspectRefundHandler(MessageRecorder recorder)
        : IEventRequestHandler<InspectRefund, RefundInspected>
    {
        public ValueTask<RefundInspected> HandleAsync(
            InspectRefund message,
            CancellationToken cancellationToken)
        {
            recorder.Record(message);

            return ValueTask.FromResult(new RefundInspected(message.RefundId, "inspected"));
        }
    }
}
