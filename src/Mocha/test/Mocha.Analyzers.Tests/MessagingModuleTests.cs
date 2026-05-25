namespace Mocha.Analyzers.Tests;

public class MessagingModuleTests
{
    [Fact]
    public async Task Generate_ExplicitModuleName_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            [assembly: MessagingModule("OrderService")]

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
    public async Task Generate_DefaultModuleName_UsesAssemblyName_MatchesSnapshot()
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
        ], assemblyName: "My.OrderService.Api").MatchMarkdownAsync();
    }
}
