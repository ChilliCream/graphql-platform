using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Rewriters;

public class MergeSelectionSetRewriterTests
{
    [Fact]
    public void Merge_Two_SelectionSets()
    {
        // arrange
        var sourceText = FileResource.Open("schema1.graphql");
        var schemaDefinition = SchemaParser.Parse(sourceText);
        var productType = (IObjectTypeDefinition)schemaDefinition.Types["Product"];

        var selectionSet1 = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            """
            {
                id
                name
                reviews {
                    id
                }
            }
            """);

        var selectionSet2 = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            """
            {
                reviews {
                    body
                }
                name
            }
            """);

        // act
        var rewriter = new MergeSelectionSetRewriter(schemaDefinition);
        var rewritten = rewriter.Merge([selectionSet1, selectionSet2], productType);

        // assert
        rewritten.MatchInlineSnapshot(
            """
            {
                id
                name
                reviews {
                    id
                    body
                }
            }
            """);
    }
}
