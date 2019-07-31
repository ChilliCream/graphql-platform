using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Tests
{
    public class QueryDocumentTests
    {
        [Fact]
        public void Create_Document_IsNull()
        {
            // arrange
            // act
            Action action = () => new QueryDocument(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Document()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            Assert.Equal(document, query.Document);
        }

        [Fact]
        public void QueryDocument_ToString()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            query.ToString().MatchSnapshot();
        }

        [Fact]
        public void QueryDocument_ToSource()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");

            // act
            var query = new QueryDocument(document);

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(query.ToSpan()))
                .ToString().MatchSnapshot();
        }
    }
}
