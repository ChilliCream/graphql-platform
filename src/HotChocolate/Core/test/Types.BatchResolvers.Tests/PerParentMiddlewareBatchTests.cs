namespace HotChocolate.Types.BatchResolvers;

public sealed partial class PerParentMiddlewareBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task Use_Should_Fail_Schema_Build_When_FieldIsBatchResolved(DeclarationStyle style)
    {
        // arrange & act, per-parent middleware has no batch-pipeline counterpart in any declaration style
        var exception = await ExpectSchemaErrorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0134", error.Code);
        Assert.Contains("PerParentMiddlewareUser.greeting", error.Message);
    }
}
