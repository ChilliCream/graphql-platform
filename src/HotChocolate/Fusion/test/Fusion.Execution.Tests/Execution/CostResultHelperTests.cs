using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.CostAnalysis;

namespace HotChocolate.Fusion.Execution;

public class CostResultHelperTests
{
    [Fact]
    public async Task AddCost_Should_PreserveSingleResult_When_EstimatesContainMultipleItems()
    {
        // arrange
        var cleanupCalled = false;
        var original = OperationResult.FromError(new Error { Message = "Original error." });
        original.RegisterForCleanup(() => cleanupCalled = true);
        CostEstimate[] estimates =
        [
            new(2, 3, null),
            new(20, 30, null)
        ];

        // act
        var result = CostResultHelper.AddCost(original, [.. estimates]);

        // assert
        Assert.Same(original, result);
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Original error."
                }
              ],
              "extensions": {
                "operationCost": {
                  "fieldCost": 2,
                  "typeCost": 3
                }
              }
            }
            """);
        await result.DisposeAsync();
        Assert.True(cleanupCalled);
    }

    [Fact]
    public async Task CreateResult_Should_ReturnCostStateInvalid_When_EstimatesAreEmpty()
    {
        // act
        var result = CostResultHelper.CreateResult([]);

        // assert
        Assert.Equal(
            new KeyValuePair<string, object?>(ExecutionContextData.ValidationErrors, true),
            Assert.Single(result.ContextData));
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The cost analysis requires a normalized operation document.",
                  "extensions": {
                    "code": "HC0048"
                  }
                }
              ]
            }
            """);
        await result.DisposeAsync();
    }
}
