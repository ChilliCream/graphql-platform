using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Directives;

public sealed class OptInFeatureStabilityDirectiveTests
{
    [Fact]
    public void From_Throws_When_Arguments_Missing()
    {
        // arrange
        var definition = new OptInFeatureStabilityMutableDirectiveDefinition(BuiltIns.String.Create());
        var directive = new Directive(definition);

        // act / assert
        Assert.Throws<InvalidOperationException>(
            () => OptInFeatureStabilityDirective.From(directive));
    }

    [Fact]
    public void OptInFeatureStabilityDirective_From_ReadsFeatureAndStability()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText(
            "A",
            // lang=graphql
            """
            schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                query: Query
            }

            type Query { field: String }

            directive @optInFeatureStability(feature: String!, stability: String!) repeatable on
                | SCHEMA
            """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);
        var result = parser.Parse();
        Assert.True(result.IsSuccess);
        var directive = Assert.Single(
            (IEnumerable<Directive>)result.Value.Directives);

        // act
        var optInFeatureStability = OptInFeatureStabilityDirective.From(directive);

        // assert
        Assert.Equal("experimental", optInFeatureStability.Feature);
        Assert.Equal("EXPERIMENTAL", optInFeatureStability.Stability);
    }
}
