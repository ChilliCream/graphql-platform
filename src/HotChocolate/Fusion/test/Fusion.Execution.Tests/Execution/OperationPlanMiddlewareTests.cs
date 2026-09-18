using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanMiddlewareTests : FusionTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_Throw_When_DocumentIsNotValidated()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddGraphQLGateway();
        FusionSetupUtilities.ClearPipeline(builder);

        // Document validation is deliberately left out of the pipeline: the normalizer
        // that OperationPlanMiddleware asks for the normalized operation requires the
        // document to already be validated.
        var executor = await builder
            .UseDocumentParser()
            .UseOperationPlan()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      foo: String
                    }
                    """))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync(
                "{ foo }",
                TestContext.Current.CancellationToken));

        // assert
        exception.Message.MatchInlineSnapshot(
            """
            The operation document must be validated before it can be normalized.
            """);
    }
}
