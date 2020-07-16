using System.IO;
using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using System.Threading.Tasks;

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
                Utf8GraphQLParser.Parse(query.AsSpan()))
                .ToString().MatchSnapshot();
        }

        [Fact]
        public async Task QueryDocument_WriteToAsync()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QueryDocument(document);
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                await query.WriteToAsync(stream);
                buffer = stream.ToArray();
            }

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(buffer))
                .ToString().MatchSnapshot();
        }

        [Fact]
        public void QueryDocument_WriteTo()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse("{ a }");
            var query = new QueryDocument(document);
            byte[] buffer;

            // act
            using (var stream = new MemoryStream())
            {
                query.WriteTo(stream);
                buffer = stream.ToArray();
            }

            // assert
            QuerySyntaxSerializer.Serialize(
                Utf8GraphQLParser.Parse(buffer))
                .ToString().MatchSnapshot();
        }
    }
}
