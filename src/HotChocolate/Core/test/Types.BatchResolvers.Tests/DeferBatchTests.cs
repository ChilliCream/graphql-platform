using HotChocolate.Execution;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class DeferBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Deliver_Data_When_RootFieldIsInsideDeferFragment(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                ready
                ... @defer {
                    productById(id: 1) { name }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot(style);
        Assert.Single(Probe.Invocations);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Deliver_Data_When_NestedFieldIsInsideDeferFragment(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);

        // act
        await using var result = await ExecuteAsync(
            executor,
            """
            {
                ready
                wrapper {
                    ... @defer {
                        productById(id: 2) { name }
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot(style);
        Assert.Single(Probe.Invocations);
    }

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Deliver_Data_When_VariableBatchRequestContainsDeferFragment(
        DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(
                """
                query($id: Int!) {
                    ready
                    ... @defer {
                        productById(id: $id) { name }
                    }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["id"] = 1 },
                    new Dictionary<string, object?> { ["id"] = 2 }
                })
            .Build();

        // act
        await using var result = await ExecuteAsync(executor, request, TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        Assert.Single(Probe.Invocations);
        Assert.Equal(new object?[] { 1, 2 }, Probe.Invocations[0].Keys.OrderBy(key => (int)key!));
        Snapshot.Create(postFix: style.ToString())
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .MatchMarkdownSnapshot();
    }
}
