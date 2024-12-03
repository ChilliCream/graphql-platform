using Xunit;

namespace HotChocolate.Language.SyntaxTree.Utilities;

public class QuerySyntaxPrinterTests
{
    [Fact]
    public void Serialize_ShortHandQueryNoIndentation_InOutShouldBeTheSame()
    {
        // arrange
        const string query =
            """
            { foo(s: "String") { bar @foo { baz @foo @bar } } }
            """;
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_OutputIsFormatted()
    {
        // arrange
        const string query =
            """
            { foo(s: "String") { bar @foo { baz @foo @bar } } }
            """;
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
    {
        // arrange
        const string query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithIndentation_OutputIsFormatted()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithoutIndentation_OutputIsOneLine()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithIndentation_CanBeParsed()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Serialize_KitchenSinkWithoutIndentation_CanBeParsed()
    {
        // arrange
        var query = FileResource.Open("kitchen-sink.graphql");
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var serializedQuery = queryDocument.ToString();

        // assert
        var parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);
        Assert.Equal(serializedQuery, parsedQuery.ToString());
    }

    [Fact]
    public void Serialize_QueryWithVarDeclaration_InOutShouldBeTheSame()
    {
        // arrange
        const string query =
            """
            query Foo($bar: [String!]!) { foo(s: "String") { bar @foo { baz @foo @bar } } }
            """;
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var serializedQuery = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, serializedQuery);
    }

    [Fact]
    public void Serialize_FragmentWithVariableDefs_InOutShouldBeTheSame()
    {
        // arrange
        var query = "fragment Foo ($bar: [String!]!) on Bar { baz }";
        var queryDocument = Utf8GraphQLParser.Parse(query,
            new ParserOptions(allowFragmentVariables: true));

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }

    // https://github.com/ChilliCream/graphql-platform/issues/1997
    [Fact]
    public void Serialize_QueryWithDirectivesOnVariables_InOutShouldBeTheSame()
    {
        // arrange
        const string query =
            """
            query Foo($variable: String @foo) { foo(a: $variable) }
            """;
        var queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }
}
