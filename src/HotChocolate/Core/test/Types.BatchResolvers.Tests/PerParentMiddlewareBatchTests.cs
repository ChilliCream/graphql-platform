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
        // arrange & act
        // per-parent field middleware (a plain .Use(...) pipeline step) has no counterpart in the
        // batch pipeline, so declaring it on a batch-resolved field is a schema error in every
        // declaration style, including source-generated: the UseWrap attribute here carries no
        // well-known middleware key, so it raises no compile-time HC0138 diagnostic.
        var exception = await ExpectSchemaErrorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0134", error.Code);
        Assert.Contains("PerParentMiddlewareUser.greeting", error.Message);
    }
}
