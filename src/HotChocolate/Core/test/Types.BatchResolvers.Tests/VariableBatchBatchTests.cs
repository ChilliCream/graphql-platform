using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class VariableBatchBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Use_PerSet_Arguments_When_RequestIsVariableBatch(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument("query($id: Int!) { productById(id: $id) { name } }")
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["id"] = 1 },
                    new Dictionary<string, object?> { ["id"] = 2 },
                    new Dictionary<string, object?> { ["id"] = 99 }
                })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Single(Probe.Invocations);
        Assert.Equal(new object?[] { 1, 2, 99 }, Probe.Invocations[0].Keys.OrderBy(key => (int)key!));
        Snapshot.Create(postFix: style.ToString())
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Set 2")
            .MatchMarkdownSnapshot();
    }

    /// <summary>
    /// Contract carried over from hc-0-1aa.10's classification of the padding-0 arm of
    /// Execution.Tests' retired BatchSelection_Should_UnionIncludedMembers_When_ConditionsDiffer:
    /// a variable batch dispatches its batch resolver once for the variable sets that are not
    /// @skip'd, and an [IsSelected] parameter is bound from the union of what every live set in
    /// that single dispatch actually selects, not from any one set's own selection.
    /// </summary>
    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Union_IsSelected_When_IncludeConditionsDifferAcrossVariableSets(
        DeclarationStyle style)
    {
        // arrange
        var log = new IsSelectedUnionLog();
        var executor = await CreateExecutorAsync(
            style, builder => builder.Services.AddSingleton(log), TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($id: Int!, $skip: Boolean!, $take: Boolean!) {
                    unionProduct(id: $id) @skip(if: $skip) {
                        id
                        left @include(if: $take)
                        right @skip(if: $take)
                    }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["id"] = 1, ["skip"] = false, ["take"] = false },
                    new Dictionary<string, object?> { ["id"] = 2, ["skip"] = false, ["take"] = true },
                    new Dictionary<string, object?> { ["id"] = 3, ["skip"] = true, ["take"] = false }
                })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        var invocation = Assert.Single(log.Invocations);
        Assert.True(invocation.Left);
        Assert.True(invocation.Right);
        Snapshot.Create(postFix: style.ToString())
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(batch.Results[2], "Skipped set")
            .MatchMarkdownSnapshot();
    }
}
