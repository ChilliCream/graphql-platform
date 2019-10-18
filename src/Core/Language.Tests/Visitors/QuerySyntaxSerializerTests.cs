
using System.IO;
using System.Text;
using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class QuerySyntaxSerializerTests
    {
        [Fact]
        public void Serialize_ShortHandQueryNoIndentation_InOutShouldBeTheSame()
        {
            // arrange
            var query = "{ foo(s: \"String\") { bar @foo " +
                "{ baz @foo @bar } } }";

            var serializer = new QuerySyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ShortHandQueryWithIndentation_OutputIsFormatted()
        {
            // arrange
            string query = "{ foo(s: \"String\") { bar @foo " +
                "{ baz @foo @bar } } }";

            var serializer = new QuerySyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
        {
            // arrange
            string query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";

            var serializer = new QuerySyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_OutputIsFormatted()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");

            var serializer = new QuerySyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);


            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_OutputIsOneLine()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");

            var serializer = new QuerySyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");

            var serializer = new QuerySyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            string serializedQuery = content.ToString();
            DocumentNode parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);

            content.Clear();
            serializer.Visit(parsedQuery, new DocumentWriter(writer));
            Assert.Equal(serializedQuery, content.ToString());

        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");

            var serializer = new QuerySyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            string serializedQuery = content.ToString();
            DocumentNode parsedQuery = Utf8GraphQLParser.Parse(serializedQuery);

            content.Clear();
            serializer.Visit(parsedQuery, new DocumentWriter(writer));
            Assert.Equal(serializedQuery, content.ToString());
        }

        [Fact]
        public void Serialize_QueryWithVarDeclaration_InOutShouldBeTheSame()
        {
            // arrange
            string query =
                "query Foo($bar: [String!]!) { foo(s: \"String\") " +
                "{ bar @foo { baz @foo @bar } } }";

            var serializer = new QuerySyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_FargmentWithVariableDefs_InOutShouldBeTheSame()
        {
            // arrange
            string query = "fragment Foo ($bar: [String!]!) on Bar { baz }";

            var serializer = new QuerySyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query,
                new ParserOptions(allowFragmentVariables: true));

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }
    }
}
