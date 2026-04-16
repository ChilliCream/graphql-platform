namespace Mocha.Analyzers.Tests;

public class MessagingMultiInterfaceTests
{
    [Fact]
    public async Task Generate_HandlerWithBatchAndEvent_RegistersAsBatch_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderEvent(int OrderId);

            // Implements both IBatchEventHandler<T> and IEventHandler<T>
            // Should register as Batch (higher priority)
            public class OrderBatchAndEventHandler : IBatchEventHandler<OrderEvent>, IEventHandler<OrderEvent>
            {
                public ValueTask HandleAsync(IMessageBatch<OrderEvent> batch, CancellationToken cancellationToken)
                    => default;

                public ValueTask HandleAsync(OrderEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
