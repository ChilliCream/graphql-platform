namespace Mocha.Analyzers.Tests;

public class MessagingBatchHandlerGeneratorTests
{
    [Fact]
    public async Task Generate_BatchEventHandler_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha;

            namespace TestApp;

            public record BulkOrderEvent(int[] OrderIds);

            public class BulkOrderHandler : IBatchEventHandler<BulkOrderEvent>
            {
                public ValueTask HandleAsync(IMessageBatch<BulkOrderEvent> batch, CancellationToken cancellationToken)
                    => default;
            }
            """
        ]).MatchMarkdownAsync();
    }
}
