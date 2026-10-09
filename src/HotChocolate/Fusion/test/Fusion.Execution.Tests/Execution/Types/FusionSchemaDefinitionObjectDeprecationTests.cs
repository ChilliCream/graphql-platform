using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionObjectDeprecationTests : FusionTestBase
{
    [Fact]
    public void Complete_Should_SetDeprecation_When_ObjectDeprecationEnabled()
    {
        // arrange
        var compositeSchemaDoc = ComposeSchemaDocument(SourceSchema);

        // act
        var schema = CreateSchema(compositeSchemaDoc, enableObjectDeprecation: true);

        // assert
        var dragon = schema.Types.GetType<FusionObjectTypeDefinition>("Dragon");
        Assert.True(dragon.IsDeprecated);
        Assert.Equal("Gone.", dragon.DeprecationReason);
        dragon.ToString().MatchInlineSnapshot(
            """
            type Dragon @deprecated(reason: "Gone.") {
              name: String
            }
            """);
    }

    [Fact]
    public void Complete_Should_NotSetDeprecation_When_ObjectDeprecationDisabled()
    {
        // arrange
        var compositeSchemaDoc = ComposeSchemaDocument(SourceSchema);

        // act
        var schema = CreateSchema(compositeSchemaDoc, enableObjectDeprecation: false);

        // assert
        var dragon = schema.Types.GetType<FusionObjectTypeDefinition>("Dragon");
        Assert.False(dragon.IsDeprecated);
        Assert.Null(dragon.DeprecationReason);
        dragon.ToString().MatchInlineSnapshot(
            """
            type Dragon {
              name: String
            }
            """);
    }

    private const string SourceSchema =
        """
        type Query { dragon: Dragon @deprecated(reason: "Use dog.") }

        type Dragon @deprecated(reason: "Gone.") { name: String }
        """;

    private static FusionSchemaDefinition CreateSchema(
        DocumentNode compositeSchemaDoc,
        bool enableObjectDeprecation)
    {
        var options = new FusionOptions { EnableObjectDeprecation = enableObjectDeprecation };
        var features = new FeatureCollection();
        features.Set<IFusionSchemaOptions>(options);

        return FusionSchemaDefinition.Create(compositeSchemaDoc, features: features);
    }
}
