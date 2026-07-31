using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionObjectDeprecationTests : FusionTestBase
{
    [Fact]
    public void Complete_Should_SetDeprecation_When_ObjectTypeIsDeprecated()
    {
        // arrange & act
        var schema = ComposeSchema(
            """
            type Query { dragon: Dragon @deprecated(reason: "Use dog.") }

            type Dragon @deprecated(reason: "Gone.") { name: String }
            """);

        // assert
        var dragon = schema.Types.GetType<FusionObjectTypeDefinition>("Dragon");
        Assert.True(dragon.IsDeprecated);
        Assert.Equal("Gone.", dragon.DeprecationReason);
    }
}
