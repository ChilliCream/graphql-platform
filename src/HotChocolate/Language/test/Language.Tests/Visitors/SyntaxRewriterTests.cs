using ChilliCream.Testing;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Language.Visitors;

public class SyntaxRewriterTests
{
    [Fact]
    public void Rename_Field()
    {
        // arrange
        DocumentNode schema = Parse(FileResource.Open("schema-kitchen-sink.graphql"));

        // act
        ISyntaxRewriter<NavigatorContext> rewriter =
            SyntaxRewriter.CreateWithNavigator(
                (node, context) =>
                {
                    if (node.Kind is SyntaxKind.FieldDefinition &&
                        "Foo".Equals(
                            context.Navigator.GetAncestor<ObjectTypeDefinitionNode>()?.Name.Value))
                    {
                        var field = (FieldDefinitionNode)node;
                        return field.WithName(field.Name.WithValue(field.Name.Value + "_abc"));
                    }

                    return node;
                });

        // assert
        schema = (DocumentNode)rewriter.Rewrite(schema, new NavigatorContext());
        schema.Print().MatchSnapshot();
    }
}
