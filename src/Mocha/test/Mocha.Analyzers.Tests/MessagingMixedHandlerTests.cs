namespace Mocha.Analyzers.Tests;

public class MessagingMixedHandlerTests
{
    [Fact]
    public async Task Generate_AllHandlerKinds_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;
            using Mocha.Sagas;

            namespace TestApp;

            // Event
            public record OrderPlacedEvent(int OrderId);
            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            // Request-Response
            public record GetOrderStatusRequest(int OrderId) : IEventRequest<string>;
            public class GetOrderStatusHandler : IEventRequestHandler<GetOrderStatusRequest, string>
            {
                public ValueTask<string> HandleAsync(GetOrderStatusRequest request, CancellationToken cancellationToken)
                    => new("ok");
            }

            // Consumer
            public record AuditLogMessage(string Action);
            public class AuditLogConsumer : IConsumer<AuditLogMessage>
            {
                public ValueTask ConsumeAsync(IConsumeContext<AuditLogMessage> context)
                    => default;
            }

            // Batch
            public record BulkOrderEvent(int[] OrderIds);
            public class BulkOrderHandler : IBatchEventHandler<BulkOrderEvent>
            {
                public ValueTask HandleAsync(IMessageBatch<BulkOrderEvent> batch, CancellationToken cancellationToken)
                    => default;
            }

            // Saga
            public class OrderState : SagaStateBase;
            public class OrderFulfillmentSaga : Saga<OrderState>
            {
                protected override void Configure(ISagaDescriptor<OrderState> descriptor) { }
            }
            """
        ]).MatchMarkdownAsync();
    }
}
