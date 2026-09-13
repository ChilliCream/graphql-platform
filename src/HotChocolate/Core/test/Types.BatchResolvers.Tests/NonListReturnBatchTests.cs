namespace HotChocolate.Types.BatchResolvers;

public sealed partial class NonListReturnBatchTests : BatchScenarioTests
{
    protected override BatchDeclarations Declarations => new()
    {
        Attribute = new Declaration(ConfigureAttribute),
        SourceGenerated = new Declaration(ConfigureSourceGenerated),
        Fluent = new Declaration(ConfigureFluent)
    };

    [Theory]
    [BatchMatrix]
    public async Task BatchResolver_Should_Fail_Schema_Build_When_ReturnTypeIsNotList(DeclarationStyle style)
    {
        // arrange & act
        var exception = await ExpectSchemaErrorAsync(style, _ => { }, TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(exception.Errors);
        Assert.Contains("must return a list type", error.Message);
        Assert.Contains("GetGreeting", error.Message);
    }
}
