namespace Mocha.Analyzers.Tests;

public class MessagingSagaGeneratorTests
{
    [Fact]
    public async Task Generate_SimpleSaga_MatchesSnapshot()
    {
        await MessagingTestHelper.GetGeneratedSourceSnapshot(
        [
            """
            using Mocha.Sagas;

            namespace TestApp;

            public class OrderState : SagaStateBase;

            public class OrderFulfillmentSaga : Saga<OrderState>
            {
                protected override void Configure(ISagaDescriptor<OrderState> descriptor)
                {
                }
            }
            """
        ]).MatchMarkdownAsync();
    }
}
