using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class SchemaParserTests
{
    [Fact]
    public void ParserSimpleObjectType()
    {
        // arrange
        var sourceText = "type a @foo(a: \"123\") " +
            "{ b: String @foo(a: \"123\") " +
            "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParserInputObjectType()
    {
        // arrange
        var sourceText = "input a @foo(a: \"123\") " +
            "{ b: String @foo(a: \"123\") c: Int = 123 }";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParserScalarType()
    {
        // arrange
        var sourceText = "scalar FOO @foo(a: \"123\")";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParserSimpleInterfaceType()
    {
        // arrange
        var sourceText = "interface a implements e @foo(a: \"123\") " +
            "{ b: String @foo(a: \"123\") " +
            "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParseEnum()
    {
        // arrange
        var sourceText = "enum Foo @foo(a: \"123\") "
            + "{ BAR @foo(a: 123) , BAZ }";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParseUnion()
    {
        // arrange
        var sourceText = "union Foo @foo(a: \"123\") = "
            + "BAR | BAZ ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParseUnion_LeadingPipe()
    {
        // arrange
        var sourceText = "union Foo @foo(a: \"123\") = "
            + "| BAR | BAZ ";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void ParseSchemaDefinition()
    {
        // arrange
        var sourceText = "\"\"\"\nDescription\n\"\"\"" +
            "schema @foo(a: \"123\") " +
            "{ query: Foo mutation: Bar subscription: Baz }";
        var parser = new Utf8GraphQLParser(
            Encoding.UTF8.GetBytes(sourceText));

        // act
        var document = parser.Parse();

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void OneGraph_Schema()
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes(FileResource.Open("onegraph.graphql"));

        // act
        var document = Utf8GraphQLParser.Parse(sourceText);

        // assert
        document.ToString().MatchSnapshot();
    }

    [Fact]
    public void Parse_Directive_With_VariableDefinition()
    {
        // arrange
        const string sourceText = "directive @foo(a: String) on VARIABLE_DEFINITION";

        // act
        var document = Utf8GraphQLParser.Parse(sourceText);

        // assert
        document.ToString().MatchSnapshot();
    }
}
