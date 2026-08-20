using HotChocolate.Language.Utilities;
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
                    if (node.Kind is SyntaxKind.FieldDefinition
                        && "Foo".Equals(
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
                        return null;
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
                        return null;
                    }

                    return node;
                });

        // assert
        DocumentNode? Fail() => (DocumentNode?)rewriter.Rewrite(schema, new NavigatorContext());
        Assert.Throws<SyntaxNodeCannotBeNullException>(Fail);
    }

    [Fact]
    public void Rewrite_DirectiveExtension_Directives()
    {
        // arrange
        var document = Parse("extend directive @foo @a");

        var rewriter = SyntaxRewriter.Create(
            node => node is DirectiveNode directive
                ? directive.WithName(directive.Name.WithValue("b"))
                : node);

        // act
        document = (DocumentNode?)rewriter.Rewrite(document, null);

        // assert
        Assert.Equal("extend directive @foo @b", document?.ToString(indented: false));
    }

    [Fact]
    public void Rewrite_Should_PreserveFragmentSpreadArguments_When_SpreadIsUnchanged()
    {
        // arrange
        var document = Parse(
            "{ ...Foo(bar: 1) }",
            new ParserOptions(new ParserOptionsExperimental(allowFragmentArguments: true)));

        // act
        var rewriter = SyntaxRewriter.Create(static node => node);
        var rewritten = (DocumentNode?)rewriter.Rewrite(document, context: null);

        // assert
        Assert.Equal("{ ...Foo(bar: 1) }", rewritten?.Print(indented: false));
    }

    [Fact]
    public void Rewrite_Should_RewriteFragmentSpreadArguments_When_ArgumentValueChanges()
    {
        // arrange
        var document = Parse(
            "{ ...Foo(bar: 1) }",
            new ParserOptions(new ParserOptionsExperimental(allowFragmentArguments: true)));

        // act
        var rewriter = SyntaxRewriter.Create(
            static node => node is IntValueNode ? new IntValueNode(2) : node);
        var rewritten = (DocumentNode?)rewriter.Rewrite(document, context: null);

        // assert
        Assert.Equal("{ ...Foo(bar: 2) }", rewritten?.Print(indented: false));
    }
}
