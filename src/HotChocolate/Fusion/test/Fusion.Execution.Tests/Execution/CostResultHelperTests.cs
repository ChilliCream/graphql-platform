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
}
