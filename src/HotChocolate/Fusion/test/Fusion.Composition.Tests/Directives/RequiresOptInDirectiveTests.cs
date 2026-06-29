using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Directives;

public sealed class RequiresOptInDirectiveTests
{
    [Fact]
    public void From_Throws_When_FeatureArgument_Missing()
    {
        // arrange
        var definition = new RequiresOptInMutableDirectiveDefinition(BuiltIns.String.Create());
        var directive = new Directive(definition);

        // act / assert
        Assert.Throws<InvalidOperationException>(
            () => RequiresOptInDirective.From(directive));
    }

    [Fact]
    public void RequiresOptInDirective_From_ReadsFeature()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText(
            "A",
            // lang=graphql
            """
            type Query {
                experimentalField: String @requiresOptIn(feature: "experimental")
            }
            """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);
        var result = parser.Parse();
        Assert.True(result.IsSuccess);
        var directive = Assert.Single(
            (IEnumerable<Directive>)result.Value.QueryType!.Fields["experimentalField"].Directives);

        // act
        var requiresOptIn = RequiresOptInDirective.From(directive);

        // assert
        Assert.Equal("experimental", requiresOptIn.Feature);
    }
}
