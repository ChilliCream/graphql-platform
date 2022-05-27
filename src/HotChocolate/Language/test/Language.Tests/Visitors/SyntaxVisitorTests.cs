using System.Collections.Generic;
using Xunit;
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

        DocumentNode schema = Parse(
            @"type Foo {
                field(a: String!): String!
            }");

        ISyntaxVisitor<NavigatorContext> visitor =
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
}
