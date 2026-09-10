using System.Text;

namespace HotChocolate.Language;

public class DirectiveParserTests
{
    [Fact]
    public void ParseUniqueDirective()
    {
        // arrange
        const string text = "directive @skip(if: Boolean!) "
            + "on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.False(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void ParseRepeatableDirective()
    {
        // arrange
        const string text = "directive @skip(if: Boolean!) repeatable "
            + "on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.True(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void ParseDescription()
    {
        // arrange
        const string text =
            // lang=graphql
            """"
            """
            Description
            """
            directive @foo(bar: String!) on FIELD_DEFINITION
            """";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.Equal("Description", directiveDefinition.Description!.Value);
    }

    [Fact]
    public void ParseDirectiveDefinitionLocation()
    {
        // arrange
        const string text = "directive @onDirective on DIRECTIVE_DEFINITION";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // act
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.Equal(
            "DIRECTIVE_DEFINITION",
            Assert.Single(directiveDefinition.Locations).Value);
    }

    [Fact]
    public void DirectiveOrderIsSignificant()
    {
        // arrange
        const string text = "type Query { field: String @a @b @c }";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        var document = parser.Parse();

        // assert
        var type = document.Definitions
            .OfType<ObjectTypeDefinitionNode>().FirstOrDefault();
        Assert.NotNull(type?.Fields.Single().Directives);
        Assert.Collection(type.Fields.Single().Directives,
            t => Assert.Equal("a", t.Name.Value),
            t => Assert.Equal("b", t.Name.Value),
            t => Assert.Equal("c", t.Name.Value));
    }

    [Fact]
    public void ParseQueryDirective()
    {
        // arrange
        const string text = @"
                query ($var: Boolean) @onQuery {
                    field
                }
            ";

        // act
        var document = Utf8GraphQLParser.Parse(
            Encoding.UTF8.GetBytes(text));

        // assert
        document.MatchSnapshot();
    }

    [Fact]
    public void ParseDirectivesOnDirectiveDefinition()
    {
        // arrange
        const string text =
            "directive @foo(arg: Int) @tag(name: \"a\") repeatable on OBJECT";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // act
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        var directive = Assert.Single(directiveDefinition.Directives);
        Assert.Equal("tag", directive.Name.Value);
        Assert.True(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void ParseDirectivesOnDirectiveDefinitionWithoutArguments()
    {
        // arrange
        const string text = "directive @foo @a @b on FIELD";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // act
        var document = parser.Parse();

        // assert
        var directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.Collection(
            directiveDefinition.Directives,
            d => Assert.Equal("a", d.Name.Value),
            d => Assert.Equal("b", d.Name.Value));
    }

    [Fact]
    public void ParseDirectiveExtension()
    {
        // arrange
        const string text = "extend directive @foo @tag(name: \"a\")";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // act
        var document = parser.Parse();

        // assert
        var extension = document.Definitions
            .OfType<DirectiveExtensionNode>().FirstOrDefault();
        Assert.NotNull(extension);
        Assert.Equal("foo", extension.Name.Value);
        var directive = Assert.Single(extension.Directives);
        Assert.Equal("tag", directive.Name.Value);
    }

    [Fact]
    public void ParseDirectiveExtensionWithoutDirectives()
    {
        // arrange
        // per the grammar, Directives[Const] is required on a directive extension
        const string text = "extend directive @foo";

        // act
        static void Action() => Utf8GraphQLParser.Parse(text);

        // assert
        Assert.Throws<SyntaxException>(Action);
    }
}
