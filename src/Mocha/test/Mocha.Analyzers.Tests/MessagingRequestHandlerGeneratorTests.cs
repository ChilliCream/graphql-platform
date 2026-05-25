namespace Mocha.Analyzers.Tests;

public class MessagingRequestHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_RequestResponseHandler_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record GetOrderStatusRequest(int OrderId) : IEventRequest<string>;

            public class GetOrderStatusHandler : IEventRequestHandler<GetOrderStatusRequest, string>
            {
                public ValueTask<string> HandleAsync(GetOrderStatusRequest request, CancellationToken cancellationToken)
                    => new("shipped");
            }
            """
        ]).MatchMarkdownAsync();
    }

    [Fact]
    public async Task Generate_SendHandler_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record ProcessOrderRequest(int OrderId);

            public class ProcessOrderHandler : IEventRequestHandler<ProcessOrderRequest>
            {
                public ValueTask HandleAsync(ProcessOrderRequest request, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
