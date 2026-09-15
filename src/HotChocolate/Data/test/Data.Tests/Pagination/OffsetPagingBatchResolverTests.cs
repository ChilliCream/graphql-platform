using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Pagination;

public class OffsetPagingBatchResolverTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(128, true)]
    [InlineData(128, false)]
    public async Task UseOffsetPaging_Should_EvaluateCountFlags_When_CompiledSelectionIsReused(
        int padding, bool includeTotalCount)
    {
        // arrange
        var log = new PagingBatchTestLog();
        var executor = await PagingBatchTestLog.CreateExecutor(true,
            new PagingOptions { IncludeTotalCount = includeTotalCount }, log);
        var (document, sets) = PagingBatchTestLog.CountRequest(true, padding, includeTotalCount);

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(document).SetVariableValues(sets).Build(), TestContext.Current.CancellationToken);
        sets.Reverse();
        await using var reversed = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument(document).SetVariableValues(sets).Build(), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(includeTotalCount ? new[] { 1, 1, 1, 1 } : [2, 2], log.Batches.Select(b => b.Length));
        var batch = Assert.IsType<OperationResultBatch>(result);
        var reverseBatch = Assert.IsType<OperationResultBatch>(reversed);
        new Snapshot(postFix: $"{padding}_{includeTotalCount}")
            .Add(batch.Results[0], "Without count")
            .Add(batch.Results[1], "With count")
            .Add(reverseBatch.Results[0], "Reused selection with count")
            .Add(reverseBatch.Results[1], "Reused selection without count")
            .Add(log.Batches, "Resolver batches")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task UseOffsetPaging_Should_SeparateAliases_When_ArgumentsMatch()
    {
        // arrange
        var log = new PagingBatchTestLog();
        var executor = await PagingBatchTestLog.CreateExecutor(true, new PagingOptions(), log);

        // act
        var result = await executor.ExecuteAsync(
            "{ a:products(id:1,take:1){items} b:products(id:2,take:1){items} }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new[] { 1, 1 }, log.Batches.Select(b => b.Length));
        new Snapshot().Add(result, "Result").Add(log.Batches, "Resolver batches").MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("default", 10, 50, false, 2)]
    [InlineData("clamped", 100, 20, false, 2)]
    [InlineData("required", 10, 50, true, 1)]
    [InlineData("different", 10, 50, false, 1)]
    [InlineData("skipZero", 10, 50, false, 1)]
    [InlineData("explicitNull", 10, 50, false, 2)]
    public async Task UseOffsetPaging_Should_NormalizeOnlyOmittedSizes_When_SelectionSpansVariableSets(
        string scenario, int defaultSize, int maxSize, bool required, int firstBatchSize)
    {
        // arrange
        var log = new PagingBatchTestLog();
        var executor = await PagingBatchTestLog.CreateExecutor(true, new PagingOptions
        {
            DefaultPageSize = defaultSize,
            MaxPageSize = maxSize,
            RequirePagingBoundaries = required
        }, log);
        var sets = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["id"] = 1 },
            new Dictionary<string, object?> { ["id"] = 2, ["take"] = Math.Min(defaultSize, maxSize) },
            new Dictionary<string, object?> { ["id"] = 3, ["take"] = maxSize + 1 }
        };
        if (scenario is "different" or "skipZero" or "explicitNull")
        {
            sets[0] = new Dictionary<string, object?> { ["id"] = 1, ["take"] = scenario == "explicitNull" ? null : 1 };
            sets[1] = new Dictionary<string, object?>
            {
                ["id"] = 2,
                ["take"] = scenario == "different" ? 2 : scenario == "explicitNull" ? 10 : 1,
                ["skip"] = scenario == "skipZero" ? 0 : null
            };
        }

        // act
        await using var result = await executor.ExecuteAsync(OperationRequestBuilder.New()
            .SetDocument("query($id:Int!,$take:Int,$skip:Int){ products(id:$id,take:$take,skip:$skip){ items pageInfo { hasNextPage hasPreviousPage } } }")
            .SetVariableValues(sets).Build(), TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Equal(firstBatchSize, log.Batches[0].Length);
        Assert.Equal(firstBatchSize == 2 ? new[] { 2, 1 } : [1, 1, 1], log.Partitions.Select(p => p.Length));
        Assert.Empty(batch.Results[required ? 1 : 0].ExpectOperationResult().Errors);
        new Snapshot(postFix: scenario)
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Invalid set")
            .Add(log.Batches, "Resolver batches")
            .Add(log.Arguments, "Per-entry published and raw arguments")
            .MatchMarkdownSnapshot();
    }
}
