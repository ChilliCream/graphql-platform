using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

public sealed class OptInFeatureStabilityDefinitionTests
{
    // The @optInFeatureStability directive is repeatable, accepts two required String
    // arguments named "feature" and "stability", and is valid on schema definitions.
    [Fact]
    public void Create_ReturnsCorrectDefinition()
    {
        // arrange
        var schema = new MutableSchemaDefinition();

        // act
        var definition = OptInFeatureStabilityMutableDirectiveDefinition.Create(schema);

        // assert
        definition.ToString().MatchInlineSnapshot(
            """
            directive @optInFeatureStability(feature: String!, stability: String!) repeatable on
              | SCHEMA
            """);
    }
}
