using HotChocolate.Features;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

public sealed class OptInFeatureStabilityDefinitionTests
{
    [Fact]
    public void SourceSchema_With_OptInFeatureStability_Parses()
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
            """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        var directive = Assert.Single((IEnumerable<Directive>)result.Value.Directives);
        Assert.Equal("optInFeatureStability", directive.Name);
        var incomplete = directive.Definition.Features.Get<IncompleteDirectiveDefinitionFeature>();
        Assert.Null(incomplete);
    }
}
