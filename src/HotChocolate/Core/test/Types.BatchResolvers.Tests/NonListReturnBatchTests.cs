using HotChocolate.Resolvers;

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
        var declaringType = style switch
        {
            DeclarationStyle.Attribute => typeof(NonListReturnUserAttributeExtension),
            DeclarationStyle.SourceGenerated => typeof(NonListReturnUserNode),
            DeclarationStyle.Fluent => typeof(FluentNonListReturnResolvers),
            _ => throw ThrowHelper.UnknownDeclarationStyle(style)
        };
        var expected = BatchResolverErrors.ReturnTypeMustBeList(declaringType, "GetGreeting").Errors[0].Message;
        var error = Assert.Single(exception.Errors);
        Assert.Contains(expected, error.Message);
    }
}
