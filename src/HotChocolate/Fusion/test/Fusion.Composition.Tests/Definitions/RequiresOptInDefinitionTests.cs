using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

public sealed class RequiresOptInDefinitionTests
{
    [Fact]
    public void SourceSchema_With_RequiresOptIn_Parses()
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

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        var directives = result.Value.QueryType!.Fields["experimentalField"].Directives;
        var directive = Assert.Single((IEnumerable<Directive>)directives);
        Assert.Equal("requiresOptIn", directive.Name);
    }
}
