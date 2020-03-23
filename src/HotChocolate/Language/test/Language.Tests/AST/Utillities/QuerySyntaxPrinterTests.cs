using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language.Utilities
{
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
            string result = queryDocument.ToString(false);

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
            string result = queryDocument.ToString(false);

            // assert
            Assert.Equal(query, result);
        }

        [Fact]
        public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
        {
            // arrange
            string query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string result = queryDocument.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_OutputIsFormatted()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string result = queryDocument.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_OutputIsOneLine()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string result = queryDocument.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string result = queryDocument.ToString();

            // assert
            result.MatchSnapshot();

        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string serializedQuery = queryDocument.ToString();

            // assert
            DocumentNode parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);
            Assert.Equal(serializedQuery, parsedQuery.ToString());
        }

        [Fact]
        public void Serialize_QueryWithVarDeclaration_InOutShouldBeTheSame()
        {
            // arrange
            string query =
                "query Foo($bar: [String!]!) { foo(s: \"String\") " +
                "{ bar @foo { baz @foo @bar } } }";
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            string serializedQuery = queryDocument.ToString(false);

            // assert
            Assert.Equal(query, serializedQuery);
        }

        [Fact]
        public void Serialize_FragmentWithVariableDefs_InOutShouldBeTheSame()
        {
            // arrange
            string query = "fragment Foo ($bar: [String!]!) on Bar { baz }";
            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query,
                new ParserOptions(allowFragmentVariables: true));

            // act
            string result = queryDocument.ToString(false);

            // assert
            Assert.Equal(query, result);
        }
    }
}
