namespace Mocha.Analyzers.Tests;

public class NotificationHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_SingleNotificationHandler_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record OrderCreated(int OrderId) : INotification;

            public class OrderCreatedEmailHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_MultipleHandlersForSameNotification_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Mediator;

            namespace TestApp;

            public record OrderCreated(int OrderId) : INotification;

            public class SendEmailHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }

            public class UpdateStatsHandler : INotificationHandler<OrderCreated>
            {
                public ValueTask HandleAsync(OrderCreated notification, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
