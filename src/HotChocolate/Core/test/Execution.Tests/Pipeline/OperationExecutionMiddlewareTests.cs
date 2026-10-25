using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

public sealed class OperationExecutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_ReturnRequestError_When_VariableBatchIsEmpty()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d
                .Field("foo")
                .Argument("bar", a => a.Type<StringType>())
                .Resolve("baz"))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var request = OperationRequestBuilder.New()
            .SetDocument("query($bar: String) { foo(bar: $bar) }")
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
