using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

public class RequestReplyTests
{
    [Fact]
    public void BuildRuntime_Should_Throw_When_ResponseProducingRequestHandlerRegistered()
    {
        // arrange
        var build = () => CreateRuntime(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("No configured transport supports", ex.Message);
        Assert.Contains("RequestReply", ex.Message);
    }

    [Fact]
    public void BuildRuntime_Should_Throw_When_MultipleResponseProducingRequestHandlersRegistered()
    {
        // arrange
        var build = () => CreateRuntime(b =>
        {
            b.AddRequestHandler<GetOrderStatusHandler>();
            b.AddRequestHandler<GetShipmentStatusHandler>();
        });

        // act
        var ex = Assert.Throws<InvalidOperationException>(build);

        // assert
        Assert.Contains("RequestReply", ex.Message);
    }

    [Fact]
    public void BuildRuntime_Should_Succeed_When_OneWaySendHandlerRegistered()
    {
        // arrange
        var build = () => CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // act
        var runtime = build();

        // assert
        var route = runtime.Router.InboundRoutes.Single(r =>
            r.Kind == InboundRouteKind.Send && r.MessageType?.RuntimeType == typeof(ProcessPayment));
        Assert.NotNull(route.Endpoint);
    }

    [Fact]
    public void DiscoverEndpoints_Should_NotCreateReplyEndpoints_When_OneWaySendHandlerRegistered()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // act
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();

        // assert
        Assert.Null(transport.ReplyReceiveEndpoint);
        Assert.Null(transport.ReplyDispatchEndpoint);
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }

    public sealed class GetShipmentStatus : IEventRequest<ShipmentStatusResponse>
    {
        public required string TrackingNumber { get; init; }
    }

    public sealed class ShipmentStatusResponse
    {
        public required string TrackingNumber { get; init; }
        public required string Status { get; init; }
    }

    public sealed class GetShipmentStatusHandler : IEventRequestHandler<GetShipmentStatus, ShipmentStatusResponse>
    {
        public ValueTask<ShipmentStatusResponse> HandleAsync(
            GetShipmentStatus request,
            CancellationToken cancellationToken)
        {
            return new(new ShipmentStatusResponse { TrackingNumber = request.TrackingNumber, Status = "InTransit" });
        }
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddEventHub(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .BuildRuntime();
    }
}
