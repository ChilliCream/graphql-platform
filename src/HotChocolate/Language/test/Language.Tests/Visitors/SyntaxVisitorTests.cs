using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Language.Visitors.SyntaxVisitor;

namespace HotChocolate.Language.Visitors;

public class SyntaxVisitorTests
{
    [Fact]
    public void Visit_With_Navigator()
    {
        // arrange
        var list = new List<string>();

        var schema = Parse(
            @"type Foo {
                field(a: String!): String!
            }");

        var visitor =
            CreateWithNavigator<NavigatorContext>(
                enter: (n, c) =>
                {
                    if (n is FieldDefinitionNode or InputValueDefinitionNode)
                    {
                        list.Add(c.Navigator.CreateCoordinate().ToString());
                    }

                    return SyntaxVisitor.Continue;
                },
                options: new() { VisitArguments = true });

        // act
        visitor.Visit(schema, new NavigatorContext());

        // assert
        Assert.Collection(
            list,
            t => Assert.Equal("Foo.field", t),
            t => Assert.Equal("Foo.field(a:)", t));
    }

    [Fact]
    public void Visit_DirectiveExtension()
    {
        // arrange
        var visited = new List<SyntaxKind>();
        var document = Parse("extend directive @foo @tag");

        var visitor = Create(
            enter: node =>
            {
                visited.Add(node.Kind);
                return SyntaxVisitor.Continue;
            },
            options: new() { VisitNames = true, VisitDirectives = true });

        // act
        visitor.Visit(document, null);

        // assert
        Assert.Contains(SyntaxKind.DirectiveExtension, visited);
        Assert.Contains(SyntaxKind.Directive, visited);
    }

    [Fact]
    public void Visit_DirectiveExtension_With_Navigator()
    {
        // arrange
        var list = new List<string>();
        var document = Parse("extend directive @foo @tag");

        var visitor =
            CreateWithNavigator<NavigatorContext>(
                enter: (n, c) =>
                {
                    if (n is DirectiveNode)
                    {
                        list.Add(c.Navigator.CreateCoordinate().ToString());
                    }

                    return SyntaxVisitor.Continue;
                },
                options: new() { VisitNames = true, VisitDirectives = true });

        // act
        visitor.Visit(document, new NavigatorContext());

        // assert
        Assert.Equal("@foo", Assert.Single(list));
    }
}
