using HotChocolate.Language.Utilities;
using Xunit;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Language.Visitors;

public class SyntaxRewriterTests
{
    [Fact]
    public void Rename_Field()
    {
        // arrange
        var schema = Parse(FileResource.Open("schema-kitchen-sink.graphql"));

        // act
        var rewriter =
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
        schema = (DocumentNode?)rewriter.Rewrite(schema, new NavigatorContext());
        schema?.Print().MatchSnapshot();
    }

    [Fact]
    public void Remove_Field()
    {
        // arrange
        var schema = Parse(@"
            schema {
              query: QueryType
              mutation: MutationType
            }

            type Foo {
              one: String!
              two: Int
              three: String!
            }

            type Bar {
              one: String!
              two: Int
              three: String!
            }
            ");

        // act
        var rewriter =
            SyntaxRewriter.CreateWithNavigator(
                (node, context) =>
                {
                    if (node.Kind is SyntaxKind.FieldDefinition
                        && ((FieldDefinitionNode)node).Name.Value.Equals("two", StringComparison.Ordinal)
                        && "Foo".Equals(context.Navigator.GetAncestor<ObjectTypeDefinitionNode>()?.Name.Value))
                    {
                        return default;
                    }

                    return node;
                });

        // assert
        schema = (DocumentNode?)rewriter.Rewrite(schema, new NavigatorContext());
        schema?.Print().MatchSnapshot();
    }

    [Fact]
    public void Remove_StringValueField_ExceptionThrown()
    {
        // arrange
        var schema = Parse(@"
            type Foo {
               abc : String
            }
            ");

        // act
        var rewriter =
            SyntaxRewriter.CreateWithNavigator(
                (node, context) =>
                {
                    if (node.Kind is SyntaxKind.Name
                        && "Foo".Equals(context.Navigator.GetAncestor<ObjectTypeDefinitionNode>()?.Name.Value))
                    {
                        return default;
                    }

                    return node;
                });

        // assert
        DocumentNode? Fail() => (DocumentNode?)rewriter.Rewrite(schema, new NavigatorContext());
        Assert.Throws<SyntaxNodeCannotBeNullException>(Fail);
    }
}
