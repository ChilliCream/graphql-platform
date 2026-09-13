namespace HotChocolate.Types.BatchResolvers;

public sealed partial class GlobalIdBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute, nameof(IdProductAttributeExtension.GetExternalId)),
        SourceGenerated = new Declaration(ConfigureSourceGenerated, nameof(IdProductNode.GetExternalId)),
        Fluent = new Declaration(ConfigureFluent, nameof(FluentIdResolvers.GetExternalId))
    };

    [Theory]
    [BatchMatrix]
    public async Task Id_Should_Encode_GlobalId_When_FieldIsBatchResolved(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);
        var first = Convert.ToBase64String("IdProduct:1"u8);
        var second = Convert.ToBase64String("IdProduct:2"u8);

        // act
        var result = await ExecuteAsync(
            executor, "{ products { externalId } }", TestContext.Current.CancellationToken);

        // assert
        Assert.Single(Probe.Invocations);
        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "products": [
                  {
                    "externalId": "{{first}}"
                  },
                  {
                    "externalId": "{{second}}"
                  }
                ]
              }
            }
            """);
    }

    [Theory]
    [BatchMatrix]
    public async Task Id_Should_Decode_GlobalId_When_BatchArgumentIsId(DeclarationStyle style)
    {
        // arrange
        var executor = await CreateExecutorAsync(
            style, _ => { }, TestContext.Current.CancellationToken);
        var first = Convert.ToBase64String("IdProduct:1"u8);
        var second = Convert.ToBase64String("IdProduct:2"u8);

        // act
        var result = await ExecuteAsync(
            executor,
            $$"""
            {
                first: productById(id: "{{first}}") { name }
                second: productById(id: "{{second}}") { name }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, Probe.Invocations.Count);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "first": {
                  "name": "Product 1"
                },
                "second": {
                  "name": "Product 2"
                }
              }
            }
            """);
    }
}
