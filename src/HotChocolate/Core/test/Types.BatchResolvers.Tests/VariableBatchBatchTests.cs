using HotChocolate.Execution;

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
}
