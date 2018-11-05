
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class QuerySerializerTests
    {
        [Fact]
        public void Serialize_ShortHandQueryNoIndentation_InOutShouldBeTheSame()
        {
            // arrange
            string query = "{ foo(s: \"String\") { bar @foo { baz @foo @bar } } }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer();

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(
                query,
                serializer.Value);
        }

        [Fact]
        public void Serialize_ShortHandQueryWithIndentation_OutputIsFormatted()
        {
            // arrange
            string query = "{ foo(s: \"String\") { bar @foo { baz @foo @bar } } }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            serializer.Value.Snapshot();
        }

        [Fact]
        public void Serialize_ShortHandQueryWithIndentation_LineBetweenFields()
        {
            // arrange
            string query = "{ foo { foo bar { foo @foo @bar bar @bar baz } } }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            serializer.Value.Snapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_OutputIsFormatted()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            serializer.Value.Snapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_OutputIsOneLine()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer();

            // act
            serializer.Visit(queryDocument);

            // assert
            serializer.Value.Snapshot();
        }

        [Fact]
        public void Serialize_KitchenSinkWithIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            string serializedQuery = serializer.Value;
            DocumentNode parsedQuery = Parser.Default.Parse(serializedQuery);
            serializer.Visit(parsedQuery);
            Assert.Equal(serializedQuery, serializer.Value);

        }

        [Fact]
        public void Serialize_KitchenSinkWithoutIndentation_CanBeParsed()
        {
            // arrange
            string query = FileResource.Open("kitchen-sink.graphql");
            DocumentNode queryDocument = Parser.Default.Parse(query);
            QuerySerializer serializer = new QuerySerializer();

            // act
            serializer.Visit(queryDocument);

            // assert
            string serializedQuery = serializer.Value;
            DocumentNode parsedQuery = Parser.Default.Parse(serializedQuery);
            serializer.Visit(parsedQuery);
            Assert.Equal(serializedQuery, serializer.Value);
        }
    }
}
