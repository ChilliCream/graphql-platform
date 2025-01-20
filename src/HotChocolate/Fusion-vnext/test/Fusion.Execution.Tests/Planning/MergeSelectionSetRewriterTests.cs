using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class MergeSelectionSetRewriterTests
{
    [Fact]
    public void Merge_Two_SelectionSets()
    {
        // arrange
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);
        var productType = compositeSchema.GetType<CompositeObjectType>("Product");

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
        var rewriter = new MergeSelectionSetRewriter(compositeSchema);
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
