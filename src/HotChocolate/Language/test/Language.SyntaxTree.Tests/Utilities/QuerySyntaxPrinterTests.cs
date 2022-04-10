using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language.SyntaxTree.Utilities;

public class QuerySyntaxPrinterTests
{
    [Fact]
    public void Serialize_ShortHandQueryNoIndentation_InOutShouldBeTheSame()
    {
        // arrange
        var query = "{ foo(s: \"String\") { bar @foo " +
            "{ baz @foo @bar } } }";
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_OutputIsFormatted()
    {
        // arrange
        var query = "{ foo(s: \"String\") { bar @foo " +
            "{ baz @foo @bar } } }";
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }

    [Fact]
    public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
    {
        // arrange
        var query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

        // act
        var serializedQuery = queryDocument.ToString();

        // assert
        DocumentNode parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);
        Assert.Equal(serializedQuery, parsedQuery.ToString());
    }

    [Fact]
    public void Serialize_QueryWithVarDeclaration_InOutShouldBeTheSame()
    {
        // arrange
        var query =
            "query Foo($bar: [String!]!) { foo(s: \"String\") " +
            "{ bar @foo { baz @foo @bar } } }";
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
        DocumentNode queryDocument = Utf8GraphQLParser.Parse(query,
            new ParserOptions(allowFragmentVariables: true));

        // act
        var result = queryDocument.ToString(false);

        // assert
        Assert.Equal(query, result);
    }
}
