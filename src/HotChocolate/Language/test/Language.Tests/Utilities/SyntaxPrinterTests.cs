using Xunit;

namespace HotChocolate.Language.Utilities;

public class SyntaxPrinterTests
{
    [Fact]
    public void Serialize_ShortHandQueryNoIndentation_InOutShouldBeTheSame()
    {
        // arrange
        var query = "{ foo(s: \"String\") { bar @foo { baz @foo @bar } } }";

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print(false);

        // assert
        Assert.Equal(query, printed);
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_OutputIsFormatted()
    {
        // arrange
        var query = "{ foo(s: \"String\") { bar @foo { baz @foo @bar } } }";

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print(true);

        // assert
        printed.MatchSnapshot();
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
    {
        // arrange
        var query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print(true);

        // assert
        printed.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithIndentation_OutputIsFormatted()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print(true);

        // assert
        printed.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithoutIndentation_OutputIsOneLine()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print(false);

        // assert
        printed.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithIndentation_CanBeParsed()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");

        // act
        var printed = Utf8GraphQLParser.Parse(query).Print();

        // assert
        var document = Utf8GraphQLParser.Parse(printed);
        Assert.Equal(printed, document.ToString());
    }

    [Fact]
    public void Serialize_KitchenSinkWithoutIndentation_CanBeParsed()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");

        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var serializedQuery = queryDocument.Print();

        // assert
        var parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);
        Assert.Equal(serializedQuery, parsedQuery.Print());
    }

    [Fact]
    public void Serialize_QueryWithVarDeclaration_InOutShouldBeTheSame()
    {
        // arrange
        var query =
            "query Foo($bar: [String!]!) { foo(s: \"String\") " +
            "{ bar @foo { baz @foo @bar } } }";

        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var printed = queryDocument.Print(false);

        // assert
        Assert.Equal(query, printed);
    }

    [Fact]
    public void Serialize_FragmentWithVariableDefs_InOutShouldBeTheSame()
    {
        // arrange
        var query = "fragment Foo ($bar: [String!]!) on Bar { baz }";

        var queryDocument = Utf8GraphQLParser.Parse(query,
            new ParserOptions(allowFragmentVariables: true));

        // act
        var printed = queryDocument.Print(false);

        // assert
        Assert.Equal(query, printed);
    }
}
