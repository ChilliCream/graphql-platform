using System.Net;
using HotChocolate.Collections.Immutable;
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
        var custom = new Dictionary<string, object?> { ["value"] = "kept" };
        var original = OperationResult.FromError(new Error { Message = "Original error." });
        original.Extensions = original.Extensions.Add("custom", custom);
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
                "custom": {
                  "value": "kept"
                },
                "operationCost": {
                  "fieldCost": 2,
                  "typeCost": 3
                }
              }
            }
            """);
        Assert.Same(custom, original.Extensions["custom"]);
        Assert.Equal(["custom", "operationCost"], original.Extensions.Keys);
        await result.DisposeAsync();
        Assert.True(cleanupCalled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateResult_Should_PreserveOrderedCostValues_When_ResponseSizeIsOptional(bool responseSize)
    {
        // arrange
        var estimate = new CostEstimate(2, 3, responseSize ? 5 : null);

        // act
        await using var result = CostResultHelper.CreateResult([estimate]);
        var operation = result.ExpectOperationResult();

        // assert
        var cost = Assert.IsType<ImmutableOrderedDictionary<string, object?>>(operation.Extensions["operationCost"]);
        Assert.Equal(
            responseSize
                ? new[] { ("fieldCost", 2d), ("typeCost", 3d), ("maxResponseSize", 5d) }
                : [("fieldCost", 2d), ("typeCost", 3d)],
            cost.Select(static pair => (pair.Key, (double)pair.Value!)));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(long.MinValue)]
    [InlineData(0x7ff8000000001234L)]
    [InlineData(0x7ff0000000000000L)]
    [InlineData(unchecked((long)0xfff0000000000000))]
    public async Task CreateResult_Should_PreserveNumericRepresentation_When_CostContainsSpecialValue(long bits)
    {
        // arrange
        var value = BitConverter.Int64BitsToDouble(bits);
        var expected = double.IsPositiveInfinity(value)
            ? (typeof(string), 0L, "Infinity")
            : (typeof(double), bits, null);

        // act
        await using var result = CostResultHelper.CreateResult([new CostEstimate(value, value, value)]);

        // assert
        var cost = Assert.IsType<ImmutableOrderedDictionary<string, object?>>(
            result.ExpectOperationResult().Extensions["operationCost"]);
        Assert.Equal(
            new[] { ("fieldCost", expected), ("typeCost", expected), ("maxResponseSize", expected) },
            cost.Select(static pair => (
                pair.Key,
                (pair.Value!.GetType(),
                    pair.Value is double number ? BitConverter.DoubleToInt64Bits(number) : 0L,
                    pair.Value as string))));
    }

    [Fact]
    public async Task AddCost_Should_PreserveExtensions_When_OperationCostAlreadyExists()
    {
        // arrange
        await using var original = OperationResult.FromError(new Error { Message = "Original error." });
        var extensions = original.Extensions.Add("operationCost", "original");
        original.Extensions = extensions;

        // act
        var exception = Record.Exception(
            () => CostResultHelper.AddCost(original, [new CostEstimate(2, 3, null)]));

        // assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Same(extensions, original.Extensions);
    }

    [Fact]
    public async Task AddCost_Should_AssociateEstimates_When_ResultIsBatch()
    {
        // arrange
        var cleanupCount = 0;
        var first = OperationResult.FromError(new Error { Message = "First." });
        var second = OperationResult.FromError(new Error { Message = "Second." });
        first.RegisterForCleanup(() => cleanupCount++);
        second.RegisterForCleanup(() => cleanupCount++);
        await using var batch = new OperationResultBatch([first, second]);

        // act
        var result = CostResultHelper.AddCost(batch, [new CostEstimate(2, 3, null), new CostEstimate(5, 7, 11)]);
        var firstCost = (ImmutableOrderedDictionary<string, object?>)first.Extensions["operationCost"]!;
        var secondCost = (ImmutableOrderedDictionary<string, object?>)second.Extensions["operationCost"]!;
        await result.DisposeAsync();

        // assert
        Assert.Same(batch, result);
        Assert.Equal(new object?[] { 2d, 3d }, firstCost.Values);
        Assert.Equal(new object?[] { 5d, 7d, 11d }, secondCost.Values);
        Assert.Equal(2, cleanupCount);
    }

    [Fact]
    public async Task AddCost_Should_AppendReportToFirstResult_When_ResultIsStream()
    {
        // arrange
        var cleanupCount = 0;
        var first = OperationResult.FromError(new Error { Message = "First." });
        var second = OperationResult.FromError(new Error { Message = "Second." });
        first.RegisterForCleanup(() => cleanupCount++);
        second.RegisterForCleanup(() => cleanupCount++);
        await using var stream = new ResponseStream(() => ReadResults(first, second));
        stream.RegisterForCleanup(() => cleanupCount++);
        stream.OnFirstResult = stream.OnFirstResult.Add(result =>
        {
            result.Extensions = result.Extensions.Add("before", "kept");
            return result;
        });

        // act
        var result = CostResultHelper.AddCost(stream, [new CostEstimate(2, 3, 5)]);
        var extensionKeys = new List<string[]>();
        var costs = new List<object?>();
        await foreach (var item in stream.ReadResultsAsync())
        {
            await using (item)
            {
                extensionKeys.Add([.. item.Extensions.Keys]);
                if (item.Extensions.TryGetValue("operationCost", out var value))
                {
                    costs.AddRange(((ImmutableOrderedDictionary<string, object?>)value!).Values);
                }
            }
        }

        await stream.DisposeAsync();

        // assert
        Assert.Same(stream, result);
        Assert.Equal(new string[][] { ["before", "operationCost"], [] }, extensionKeys);
        Assert.Equal(new object?[] { 2d, 3d, 5d }, costs);
        Assert.Equal(3, cleanupCount);

        static async IAsyncEnumerable<OperationResult> ReadResults(OperationResult first, OperationResult second)
        {
            await Task.Yield();
            yield return first;
            yield return second;
        }
    }

    [Fact]
    public async Task CreateResult_Should_ReturnCostStateInvalid_When_EstimatesAreEmpty()
    {
        // act
        var result = CostResultHelper.CreateResult([]);

        // assert
        // An invalid cost-analysis state is an internal error, not a request error, so this
        // does not carry the validation-error context data and instead maps to a 500 directly.
        Assert.Equal(
            new KeyValuePair<string, object?>(
                ExecutionContextData.HttpStatusCode,
                HttpStatusCode.InternalServerError),
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
