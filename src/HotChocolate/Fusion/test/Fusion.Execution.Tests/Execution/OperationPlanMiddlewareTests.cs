using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanMiddlewareTests : FusionTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_ReturnRequestError_When_NormalizedOperationIsMissing()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddGraphQLGateway();
        FusionSetupUtilities.ClearPipeline(builder);

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
        var result = await executor.ExecuteAsync(
            "{ foo }",
            TestContext.Current.CancellationToken);
        var operationResult = result.ExpectOperationResult();

        // assert
        Assert.Equal(
            new KeyValuePair<string, object?>(ExecutionContextData.ValidationErrors, true),
            Assert.Single(operationResult.ContextData));
        operationResult.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The operation planner requires a normalized operation document.",
                  "extensions": {
                    "code": "HC0015"
                  }
                }
              ]
            }
            """);
    }
}
