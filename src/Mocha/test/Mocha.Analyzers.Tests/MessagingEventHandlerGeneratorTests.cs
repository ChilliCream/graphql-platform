namespace Mocha.Analyzers.Tests;

public class MessagingEventHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_SingleEventHandler_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_MultipleEventHandlers_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record OrderPlacedEvent(int OrderId);

            public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }

            public class OrderPlacedAuditHandler : IEventHandler<OrderPlacedEvent>
            {
                public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
