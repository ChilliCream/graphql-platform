using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

public class OperationExecutionMiddlewareTests : FusionTestBase
{
    private const string SchemaText =
        """
        type Query {
            field(input: String!): String
        }
        """;

    [Fact]
    public async Task InvokeAsync_Should_ReturnRequestError_When_VariableBatchIsEmpty()
    {
        // arrange
        // with the cost analyzer skipped, the empty batch reaches operation execution
        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .ModifyCostOptions(o => o.SkipAnalyzer = true)
            .AddInMemoryConfiguration(ComposeSchemaDocument(SchemaText))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var request = OperationRequestBuilder.New()
            .SetDocument("query($input: String!) { field(input: $input) }")
            .SetVariableValues("[]")
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.ContextData.ContainsKey(ExecutionContextData.ValidationErrors));
        operationResult.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "A variable batch request must contain at least one variable set."
                }
              ]
            }
            """);
    }
}
