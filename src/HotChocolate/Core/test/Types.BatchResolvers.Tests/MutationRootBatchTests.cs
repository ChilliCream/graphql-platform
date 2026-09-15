namespace HotChocolate.Types.BatchResolvers;

public sealed partial class MutationRootBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = Declaration.NotApplicable(SourceGeneratedNotApplicableReason),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Fail_Schema_Build_When_DeclaredOnMutationRootField(DeclarationStyle style)
    {
        // arrange
        if (GetNotApplicableReason(style) is not null)
        {
            return;
        }

        // act
        var exception = await ExpectSchemaErrorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // assert
        // "HC0135" is ErrorCodes.Schema.BatchResolverOnMutationField (Primitives/ErrorCodes.cs);
        // referencing the type directly is ambiguous here because the Types.Analyzers project,
        // wired in as a Roslyn analyzer, also exposes its own HotChocolate.ErrorCodes type.
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Contains("Mutation.appendLog", error.Message);

        if (style is DeclarationStyle.Attribute)
        {
            Assert.Contains(
                $"{typeof(MutationRootAttributeMutation).FullName}.{nameof(MutationRootAttributeMutation.AppendLog)}",
                error.Message);
        }
    }
}
