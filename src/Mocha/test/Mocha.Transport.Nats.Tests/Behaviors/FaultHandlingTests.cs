using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Transport.Nats.Tests.Fixtures;
using Mocha.Transport.Nats.Tests.Helpers;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Xunit;

namespace Mocha.Transport.Nats.Tests.Behaviors;

[Collection(JetStreamCollection.Name)]
public class FaultHandlingTests(JetStreamFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task RequestAsync_Should_ThrowRemoteError_When_HandlerThrows()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ThrowingRequestHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        var exception = await Assert.ThrowsAsync<RemoteErrorException>(
            async () => await messageBus.RequestAsync(
                new GetOrderStatus { OrderId = "ORD-FAIL" },
                CancellationToken.None));

        // assert
        Assert.Contains("InvalidOperationException", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_Should_NotAffectOtherHandlers_When_OneHandlerThrows()
    {
        // arrange
        // Each handler gets its own durable, so one failing must not stall the others. This is the
        // shape of a service that handles several message types on one bus.
        var throwingRecorder = new MessageRecorder();
        var normalRecorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddKeyedSingleton("throwing", throwingRecorder)
            .AddKeyedSingleton("shipment", normalRecorder)
            .AddMessageBus()
            .AddEventHandler<ThrowingEventHandler>()
            .AddEventHandler<ItemShippedKeyedHandler>()
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-FAIL" }, CancellationToken.None);
        await throwingRecorder.WaitAsync(TimeSpan.FromSeconds(5));

        await messageBus.PublishAsync(new ItemShipped { TrackingNumber = "TRK-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await normalRecorder.WaitAsync(s_timeout),
            "Normal handler did not receive its event after a different handler threw");
    }

    [Fact]
    public async Task SendAsync_Should_LandOnTheErrorSubject_When_TheHandlerAlwaysFails()
    {
        // arrange
        // Send and Publish converge on one subject, so the faulted copy has to reach the error subject
        // from either entry point.
        var recorder = new MessageRecorder();
        await using var scope = await fixture.CreateScopeAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(fixture.Connection)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddResilience(policy => policy.Default().Retry(1).ThenDeadLetter())
            .AddNats(nats => nats.StreamName(scope.StreamName))
            .BuildTestBusAsync();

        var messageBus = bus.CreateBus(out var busScope);
        using var _ = busScope;

        var stream = Assert.Single(bus.Topology.Streams);
        var errorSubject = Assert.Single(
            bus.Topology.Subjects.Select(s => s.Subject),
            s => s.EndsWith("_error", StringComparison.Ordinal));

        // act
        await messageBus.SendAsync(new OrderCreated { OrderId = "ORD-SEND-FAIL" }, CancellationToken.None);

        Assert.True(await recorder.WaitAsync(s_timeout), "The handler never ran.");

        // assert
        var body = await WaitForSubjectBodyAsync(stream.Name, errorSubject, s_timeout);

        Assert.NotNull(body);

        // The faulted copy keeps the original payload, so the error subject is replayable.
        Assert.Contains("ORD-SEND-FAIL", body);
    }

    private async Task<string?> WaitForSubjectBodyAsync(
        string streamName,
        string subject,
        TimeSpan timeout)
    {
        var jetStream = fixture.JetStream;
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var stream = await jetStream.GetStreamAsync(streamName, cancellationToken: CancellationToken.None);

            try
            {
                var message = await stream.GetAsync(
                    new StreamMsgGetRequest { LastBySubj = subject },
                    cancellationToken: CancellationToken.None);

                if (!message.Message.Data.IsEmpty)
                {
                    return Encoding.UTF8.GetString(message.Message.Data.Span);
                }
            }
            catch (NatsJSApiException)
            {
                // No message on the subject yet.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), CancellationToken.None);
        }

        return null;
    }

    public sealed class ItemShipped
    {
        public required string TrackingNumber { get; init; }
    }

    public sealed class ThrowingEventHandler([FromKeyedServices("throwing")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }

    public sealed class ItemShippedKeyedHandler([FromKeyedServices("shipment")] MessageRecorder recorder)
        : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class ThrowingOrderHandler(MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }

    public sealed class ThrowingRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
        {
            recorder.Record(request);
            throw new InvalidOperationException("Request handler failed deliberately");
        }
    }
}
